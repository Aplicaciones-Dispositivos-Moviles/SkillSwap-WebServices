using System.Net;
using System.Net.Http.Json;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;
using SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Integration;

public class AssessmentAttemptsEndpointsTests : ApiTestBase
{
    private const string Skill = "networking-basics";

    private static async Task<(SignedInUser Ana, LearningPathResource Path, AssessmentBlueprintResource Blueprint)>
        AnaWithAnAssessmentAsync()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var path = await AssessmentFlow.DeclareGoalAsync(ana);
        var blueprint = await AssessmentFlow.RequestBlueprintAsync(ana, AssessmentFlow.NodeId(path, Skill));
        return (ana, path, blueprint);
    }

    // ---------- Submit: approved ----------

    [Fact]
    public async Task Submit_WithAllCorrectAnswers_Returns201PassedAndCompletesTheNode()
    {
        var (ana, _, blueprint) = await AnaWithAnAssessmentAsync();

        var response = await AssessmentFlow.SubmitAsync(ana, blueprint.Id, AssessmentFlow.Answers(blueprint, 5));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var attempt = await AssessmentFlow.ReadAsync<AssessmentAttemptResource>(response);
        Assert.Equal(blueprint.Id, attempt.BlueprintId);
        Assert.Equal(ana.Id, attempt.StudentId);
        Assert.Equal(5, attempt.Score);
        Assert.Equal(5, attempt.TotalQuestions);
        Assert.True(attempt.Passed);
        Assert.Null(attempt.VerificationCaseId);
        Assert.Equal(DateTimeKind.Utc, attempt.CompletedAt.Kind);
        Assert.EndsWith($"/api/v1/assessment-attempts/{attempt.Id}", response.Headers.Location!.ToString());

        var path = await AssessmentFlow.GetPathAsync(ana);
        Assert.Equal("Completed", AssessmentFlow.NodeStatus(path, Skill));
    }

    [Fact]
    public async Task Submit_WithExactlyFourCorrectAnswers_Passes()
    {
        var (ana, _, blueprint) = await AnaWithAnAssessmentAsync();

        var attempt = await AssessmentFlow.SubmitOkAsync(ana, blueprint, 4);

        Assert.True(attempt.Passed);
        Assert.Equal(4, attempt.Score);
    }

    // ---------- Submit: failed ----------

    [Fact]
    public async Task Submit_WithThreeCorrectAnswers_Returns201OpensAPendingCaseAndKeepsTheNodeAvailable()
    {
        var (ana, _, blueprint) = await AnaWithAnAssessmentAsync();

        var attempt = await AssessmentFlow.SubmitOkAsync(ana, blueprint, 3);

        Assert.False(attempt.Passed);
        Assert.Equal(3, attempt.Score);
        Assert.NotNull(attempt.VerificationCaseId);
        Assert.Equal("Pending", attempt.VerificationCaseStatus);

        var path = await AssessmentFlow.GetPathAsync(ana);
        Assert.Equal("Available", AssessmentFlow.NodeStatus(path, Skill));
    }

    [Fact]
    public async Task Submit_DoesNotExposeTheAnswersTheStudentChose()
    {
        var (ana, _, blueprint) = await AnaWithAnAssessmentAsync();

        var response = await AssessmentFlow.SubmitAsync(ana, blueprint.Id, AssessmentFlow.Answers(blueprint, 3));

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("selectedAnswers", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("correct", body, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Submit: rejections ----------

    [Fact]
    public async Task Submit_WithTheWrongNumberOfAnswers_Returns400()
    {
        var (ana, _, blueprint) = await AnaWithAnAssessmentAsync();

        var response = await AssessmentFlow.SubmitAsync(ana, blueprint.Id, [1, 2, 3]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidAnswers", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Submit_WithAnAnswerOutOfRange_Returns400()
    {
        var (ana, _, blueprint) = await AnaWithAnAssessmentAsync();

        var response = await AssessmentFlow.SubmitAsync(ana, blueprint.Id, [1, 2, 3, 0, 4]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidAnswers", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Submit_WithoutAnswers_Returns400()
    {
        var (ana, _, blueprint) = await AnaWithAnAssessmentAsync();

        var response = await AssessmentFlow.SubmitAsync(ana, blueprint.Id, null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidAnswers", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Submit_ForAnUnknownBlueprint_Returns404()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await AssessmentFlow.SubmitAsync(ana, 9999, [1, 2, 3, 0, 1]);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("BlueprintNotFound", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Submit_ForAnotherStudentsBlueprint_Returns403()
    {
        var (_, _, blueprint) = await AnaWithAnAssessmentAsync();
        var bob = await TestApi.RegisterStudentAsync("bob");

        var response = await AssessmentFlow.SubmitAsync(bob, blueprint.Id, AssessmentFlow.Answers(blueprint, 5));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NotBlueprintOwner", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Submit_ForAnOutdatedBlueprint_Returns409()
    {
        var (ana, path, first) = await AnaWithAnAssessmentAsync();
        await AssessmentFlow.RequestBlueprintAsync(ana, AssessmentFlow.NodeId(path, Skill));

        var response = await AssessmentFlow.SubmitAsync(ana, first.Id, AssessmentFlow.Answers(first, 5));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("BlueprintOutdated", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Submit_AfterTheNodeWasCompleted_Returns409()
    {
        var (ana, _, blueprint) = await AnaWithAnAssessmentAsync();
        await AssessmentFlow.SubmitOkAsync(ana, blueprint, 5);

        var response = await AssessmentFlow.SubmitAsync(ana, blueprint.Id, AssessmentFlow.Answers(blueprint, 5));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("NodeNotAvailable", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Submit_WhileTheNodeHasAnOpenCase_Returns409()
    {
        var (ana, path, first) = await AnaWithAnAssessmentAsync();
        await AssessmentFlow.SubmitOkAsync(ana, first, 0);
        var second = await AssessmentFlow.RequestBlueprintAsync(ana, AssessmentFlow.NodeId(path, Skill));

        var response = await AssessmentFlow.SubmitAsync(ana, second.Id, AssessmentFlow.Answers(second, 5));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("OpenCaseAlreadyExists", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Submit_AsACoordinator_Returns403()
    {
        var admin = await TestApi.RegisterCoordinatorAsync("admin");

        var response = await AssessmentFlow.SubmitAsync(admin, 1, [1, 2, 3, 0, 1]);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WithoutAToken_Returns401()
    {
        var response = await TestApi.CreateClient().PostAsJsonAsync(AssessmentFlow.AttemptsUrl,
            new SubmitAssessmentAttemptResource(1, [1, 2, 3, 0, 1]));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Get by id ----------

    [Fact]
    public async Task Get_ByTheOwner_Returns200()
    {
        var (ana, _, blueprint) = await AnaWithAnAssessmentAsync();
        var submitted = await AssessmentFlow.SubmitOkAsync(ana, blueprint, 4);

        var response = await ana.Client.GetAsync($"{AssessmentFlow.AttemptsUrl}/{submitted.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var attempt = await AssessmentFlow.ReadAsync<AssessmentAttemptResource>(response);
        Assert.Equal(submitted.Id, attempt.Id);
        Assert.Equal(4, attempt.Score);
        Assert.True(attempt.Passed);
    }

    [Fact]
    public async Task Get_ByAnotherStudent_Returns403()
    {
        var (ana, _, blueprint) = await AnaWithAnAssessmentAsync();
        var submitted = await AssessmentFlow.SubmitOkAsync(ana, blueprint, 4);
        var bob = await TestApi.RegisterStudentAsync("bob");

        var response = await bob.Client.GetAsync($"{AssessmentFlow.AttemptsUrl}/{submitted.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NotAttemptOwner", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Get_ByACoordinator_Returns200()
    {
        var (ana, _, blueprint) = await AnaWithAnAssessmentAsync();
        var submitted = await AssessmentFlow.SubmitOkAsync(ana, blueprint, 4);
        var admin = await TestApi.RegisterCoordinatorAsync("admin");

        var response = await admin.Client.GetAsync($"{AssessmentFlow.AttemptsUrl}/{submitted.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_ForAnUnknownAttempt_Returns404()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await ana.Client.GetAsync($"{AssessmentFlow.AttemptsUrl}/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("AttemptNotFound", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Get_WithoutAToken_Returns401()
    {
        var response = await TestApi.CreateClient().GetAsync($"{AssessmentFlow.AttemptsUrl}/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}