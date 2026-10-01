using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Platform.LearningPathEngine.Domain.Repositories;
using SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Integration;

public class AssessmentBlueprintEndpointsTests : ApiTestBase
{
    private const string RestAndJwt = "quiero aprender a construir APIs REST con autenticación JWT";

    private static async Task<LearningPathResource> DeclareAsync(SignedInUser user)
    {
        var response = await user.Client.PostAsJsonAsync("/api/v1/learning-paths", new DeclareGoalResource(RestAndJwt));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LearningPathResource>())!;
    }

    private static int NodeId(LearningPathResource path, string skillTag)
    {
        return path.Nodes.Single(n => n.SkillTag == skillTag).Id;
    }

    private static Task<HttpResponseMessage> RequestAsync(HttpClient client, int nodeId)
    {
        return client.PostAsync($"/api/v1/path-nodes/{nodeId}/assessment-blueprint", null);
    }

    private static async Task<ProblemDetails> ProblemOf(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
    }

    // ---------- Happy path ----------

    [Fact]
    public async Task Generate_ForAnAvailableNode_Returns201WithFiveQuestionsAndNoCorrectAnswers()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var nodeId = NodeId(await DeclareAsync(ana), "networking-basics");

        var response = await RequestAsync(ana.Client, nodeId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("correct", body, StringComparison.OrdinalIgnoreCase);

        var blueprint = (await response.Content.ReadFromJsonAsync<AssessmentBlueprintResource>())!;
        Assert.Equal(nodeId, blueprint.PathNodeId);
        Assert.Equal("networking-basics", blueprint.SkillTag);
        Assert.Equal("Networking basics", blueprint.SkillName);
        Assert.Equal(5, blueprint.Questions.Count);
        Assert.All(blueprint.Questions, q => Assert.Equal(4, q.Answers.Count));
    }

    [Fact]
    public async Task Generate_PointsTheNodeToTheBlueprintAndKeepsTheCorrectAnswersOnTheServer()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var nodeId = NodeId(await DeclareAsync(ana), "networking-basics");

        var blueprint = (await (await RequestAsync(ana.Client, nodeId))
            .Content.ReadFromJsonAsync<AssessmentBlueprintResource>())!;

        var path = (await ana.Client.GetFromJsonAsync<LearningPathResource>($"/api/v1/learning-paths/{ana.Id}"))!;
        Assert.Equal(blueprint.Id, path.Nodes.Single(n => n.Id == nodeId).AssessmentBlueprintId);

        using var scope = TestApi.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<IAssessmentBlueprintRepository>()
            .FindLatestByPathNodeIdAsync(nodeId, default);
        Assert.Equal(5, stored!.Questions.Count);
        Assert.All(stored.Questions, q => Assert.InRange(q.CorrectAnswer, 0, 3));
    }

    [Fact]
    public async Task Generate_Again_ReturnsDifferentQuestionsAndPointsToTheNewBlueprint()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var nodeId = NodeId(await DeclareAsync(ana), "networking-basics");

        var first = (await (await RequestAsync(ana.Client, nodeId)).Content.ReadFromJsonAsync<AssessmentBlueprintResource>())!;
        var second = (await (await RequestAsync(ana.Client, nodeId)).Content.ReadFromJsonAsync<AssessmentBlueprintResource>())!;

        Assert.NotEqual(first.Id, second.Id);
        Assert.Empty(first.Questions.Select(q => q.Question).Intersect(second.Questions.Select(q => q.Question)));
        var path = (await ana.Client.GetFromJsonAsync<LearningPathResource>($"/api/v1/learning-paths/{ana.Id}"))!;
        Assert.Equal(second.Id, path.Nodes.Single(n => n.Id == nodeId).AssessmentBlueprintId);
    }

    // ---------- Rejections ----------

    [Fact]
    public async Task Generate_ForALockedNode_Returns409ListingThePendingPrerequisites()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var nodeId = NodeId(await DeclareAsync(ana), "rest-api-design");

        var response = await RequestAsync(ana.Client, nodeId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await ProblemOf(response);
        Assert.Equal("NodeLocked", problem.Title);
        var pending = ((JsonElement)problem.Extensions["pendingPrerequisites"]!).EnumerateArray()
            .Select(e => e.GetString()).ToList();
        Assert.Equal(["http-basics", "programming-fundamentals"], pending);
        Assert.Empty(TestApi.QuestionGenerator.Requests);
    }

    [Fact]
    public async Task Generate_ForAnotherStudentsNode_Returns403()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var bob = await TestApi.RegisterStudentAsync("bob");
        var nodeId = NodeId(await DeclareAsync(ana), "networking-basics");

        var response = await RequestAsync(bob.Client, nodeId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NotPathOwner", (await ProblemOf(response)).Title);
        Assert.Empty(TestApi.QuestionGenerator.Requests);
    }

    [Fact]
    public async Task Generate_ForAnUnknownNode_Returns404()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await RequestAsync(ana.Client, 9999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("NodeNotFound", (await ProblemOf(response)).Title);
    }

    [Fact]
    public async Task Generate_WithoutToken_Returns401()
    {
        var response = await RequestAsync(TestApi.CreateClient(), 1);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Generate_AsCoordinator_Returns403()
    {
        var coordinator = await TestApi.RegisterCoordinatorAsync("coord");

        var response = await RequestAsync(coordinator.Client, 1);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- AI failures ----------

    [Fact]
    public async Task Generate_WhenTheAiServiceFails_Returns503AndLeavesTheNodeUnchanged()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var nodeId = NodeId(await DeclareAsync(ana), "networking-basics");
        TestApi.QuestionGenerator.ExceptionToThrow = new HttpRequestException("AI service unavailable");

        var response = await RequestAsync(ana.Client, nodeId);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("QuestionGenerationFailed", (await ProblemOf(response)).Title);

        var node = (await ana.Client.GetFromJsonAsync<LearningPathResource>($"/api/v1/learning-paths/{ana.Id}"))!
            .Nodes.Single(n => n.Id == nodeId);
        Assert.Equal("Available", node.Status);
        Assert.Null(node.AssessmentBlueprintId);
    }

    [Fact]
    public async Task Generate_WhenTheAiReturnsTheWrongNumberOfQuestions_Returns503()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var nodeId = NodeId(await DeclareAsync(ana), "networking-basics");
        TestApi.QuestionGenerator.QuestionCount = 4;

        var response = await RequestAsync(ana.Client, nodeId);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}