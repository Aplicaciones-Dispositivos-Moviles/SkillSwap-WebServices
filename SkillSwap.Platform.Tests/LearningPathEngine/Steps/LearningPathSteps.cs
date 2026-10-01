using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Reqnroll;
using SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Iam.Steps;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Steps;

[Binding]
public class LearningPathSteps(ApiScenarioContext context)
{
    private const string PathsUrl = "/api/v1/learning-paths";

    // Reqnroll creates one instance of this class per scenario, so these fields live as long as it does.
    private List<string>? _lastQuestions;
    private List<string>? _previousQuestions;

    private static List<string> SplitList(string text)
    {
        return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private async Task<HttpResponseMessage> PostGoalAsync(string username, string goal)
    {
        return await context.Users[username].Client.PostAsJsonAsync(PathsUrl, new DeclareGoalResource(goal));
    }

    private async Task<LearningPathResource> GetPathAsync(string actor, string owner)
    {
        var response = await context.Users[actor].Client.GetAsync($"{PathsUrl}/{context.Users[owner].Id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LearningPathResource>())!;
    }

    private async Task RequestAssessmentAsync(string actor, string skillTag, string pathOwner)
    {
        var path = await GetPathAsync(pathOwner, pathOwner);
        var nodeId = path.Nodes.Single(n => n.SkillTag == skillTag).Id;

        context.Response = await context.Users[actor].Client
            .PostAsync($"/api/v1/path-nodes/{nodeId}/assessment-blueprint", null);

        if (context.Response.StatusCode == HttpStatusCode.Created)
        {
            var blueprint = await context.ReadBodyAsync<AssessmentBlueprintResource>();
            _previousQuestions = _lastQuestions;
            _lastQuestions = blueprint.Questions.Select(q => q.Question).ToList();
        }
    }

    // ---------- Given / When ----------

    [Given("{string} has declared the goal {string}")]
    public async Task GivenHasDeclaredTheGoal(string username, string goal)
    {
        context.Response = await PostGoalAsync(username, goal);
        Assert.Equal(HttpStatusCode.Created, context.Response.StatusCode);
    }

    [When("{string} declares the goal {string}")]
    public async Task WhenDeclaresTheGoal(string username, string goal)
    {
        context.Response = await PostGoalAsync(username, goal);
    }

    [When("{string} consults the learning path of {string}")]
    public async Task WhenConsultsTheLearningPathOf(string actor, string owner)
    {
        context.Response = await context.Users[actor].Client.GetAsync($"{PathsUrl}/{context.Users[owner].Id}");
    }

    [Given("the question generation service is unavailable")]
    public void GivenTheQuestionGenerationServiceIsUnavailable()
    {
        TestApi.QuestionGenerator.ExceptionToThrow = new HttpRequestException("AI service unavailable");
    }

    [When("{string} requests the assessment of the skill {string}")]
    public async Task WhenRequestsTheAssessmentOfTheSkill(string username, string skillTag)
    {
        await RequestAssessmentAsync(username, skillTag, username);
    }

    [Given("{string} has requested the assessment of the skill {string}")]
    public async Task GivenHasRequestedTheAssessmentOfTheSkill(string username, string skillTag)
    {
        await RequestAssessmentAsync(username, skillTag, username);
        Assert.Equal(HttpStatusCode.Created, context.Response!.StatusCode);
    }

    [When("{string} requests the assessment of the skill {string} from the path of {string}")]
    public async Task WhenRequestsTheAssessmentFromThePathOf(string actor, string skillTag, string owner)
    {
        await RequestAssessmentAsync(actor, skillTag, owner);
    }

    // ---------- Then ----------

    [Then("the path contains the skills {string} in this order")]
    public async Task ThenThePathContainsTheSkillsInThisOrder(string skills)
    {
        var path = await context.ReadBodyAsync<LearningPathResource>();
        Assert.Equal(SplitList(skills), path.Nodes.OrderBy(n => n.Order).Select(n => n.SkillTag));
    }

    [Then("the available skills are {string}")]
    public async Task ThenTheAvailableSkillsAre(string skills)
    {
        var path = await context.ReadBodyAsync<LearningPathResource>();
        Assert.Equal(SplitList(skills),
            path.Nodes.OrderBy(n => n.Order).Where(n => n.Status == "Available").Select(n => n.SkillTag));
    }

    [Then("the blueprint has {int} questions with {int} answers each")]
    public async Task ThenTheBlueprintHasQuestionsWithAnswers(int questions, int answers)
    {
        var blueprint = await context.ReadBodyAsync<AssessmentBlueprintResource>();
        Assert.Equal(questions, blueprint.Questions.Count);
        Assert.All(blueprint.Questions, q => Assert.Equal(answers, q.Answers.Count));
    }

    [Then("the blueprint does not reveal the correct answers")]
    public async Task ThenTheBlueprintDoesNotRevealTheCorrectAnswers()
    {
        var body = await context.Response!.Content.ReadAsStringAsync();
        Assert.DoesNotContain("correct", body, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the pending prerequisites are {string}")]
    public async Task ThenThePendingPrerequisitesAre(string skills)
    {
        var problem = await context.ReadBodyAsync<ProblemDetails>();
        var pending = ((JsonElement)problem.Extensions["pendingPrerequisites"]!).EnumerateArray()
            .Select(e => e.GetString()).ToList();
        Assert.Equal(SplitList(skills), pending);
    }

    [Then("the new questions differ from the previous ones")]
    public void ThenTheNewQuestionsDifferFromThePreviousOnes()
    {
        Assert.NotNull(_previousQuestions);
        Assert.NotNull(_lastQuestions);
        Assert.Empty(_previousQuestions.Intersect(_lastQuestions));
    }

    [Then("{string} sees no assessment for the skill {string}")]
    public async Task ThenSeesNoAssessmentForTheSkill(string username, string skillTag)
    {
        var node = (await GetPathAsync(username, username)).Nodes.Single(n => n.SkillTag == skillTag);
        Assert.Equal("Available", node.Status);
        Assert.Null(node.AssessmentBlueprintId);
    }
}