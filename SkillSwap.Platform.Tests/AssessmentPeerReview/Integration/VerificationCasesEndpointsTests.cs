using System.Net;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Support;
using SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Integration;

public class VerificationCasesEndpointsTests : ApiTestBase
{
    private const string Skill = "networking-basics";
    private const string Notes = "Solid understanding of the network layers.";

    /// <summary>
    ///     A student with a path who fails the assessment, which opens a verification case.
    /// </summary>
    private static async Task<(SignedInUser Ana, AssessmentBlueprintResource Blueprint, AssessmentAttemptResource Attempt)>
        AnaFailsAsync()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        await AssessmentFlow.DeclareGoalAsync(ana);
        var (blueprint, attempt) = await AssessmentFlow.FailSkillAsync(ana, Skill);
        return (ana, blueprint, attempt);
    }

    private static async Task<VerificationCaseDetailResource> DetailAsync(SignedInUser user, int caseId)
    {
        var response = await AssessmentFlow.GetCaseAsync(user, caseId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await AssessmentFlow.ReadAsync<VerificationCaseDetailResource>(response);
    }

    // ---------- Assignment (US24) ----------

    [Fact]
    public async Task FailedAttempt_WithAnAvailableVerifier_AssignsTheCaseToThem()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");

        var (ana, _, attempt) = await AnaFailsAsync();

        Assert.Equal("Assigned", attempt.VerificationCaseStatus);
        var response = await bob.Client.GetAsync(AssessmentFlow.CasesUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cases = await AssessmentFlow.ReadAsync<List<VerificationCaseResource>>(response);
        var assigned = Assert.Single(cases);
        Assert.Equal(attempt.VerificationCaseId, assigned.Id);
        Assert.Equal(ana.Id, assigned.StudentId);
        Assert.Equal(bob.Id, assigned.VerifierUserId);
        Assert.Equal(Skill, assigned.SkillTag);
        Assert.Equal("Assigned", assigned.Status);
    }

    [Fact]
    public async Task FailedAttempt_WithoutVerifiers_LeavesTheCasePending()
    {
        var (ana, _, attempt) = await AnaFailsAsync();

        var detail = await DetailAsync(ana, attempt.VerificationCaseId!.Value);

        Assert.Equal("Pending", detail.Case.Status);
        Assert.Null(detail.Case.VerifierUserId);
    }

    [Fact]
    public async Task FailedAttempt_NeverGoesToAnUnavailableVerifier()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        await AssessmentFlow.SetAvailabilityAsync(bob, false);

        var (ana, _, attempt) = await AnaFailsAsync();

        Assert.Equal("Pending", (await DetailAsync(ana, attempt.VerificationCaseId!.Value)).Case.Status);
    }

    [Fact]
    public async Task PendingCase_IsAssignedWhenAVerifierBecomesAvailable()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        await AssessmentFlow.SetAvailabilityAsync(bob, false);
        var (ana, _, attempt) = await AnaFailsAsync();

        await AssessmentFlow.SetAvailabilityAsync(bob, true);

        var detail = await DetailAsync(ana, attempt.VerificationCaseId!.Value);
        Assert.Equal("Assigned", detail.Case.Status);
        Assert.Equal(bob.Id, detail.Case.VerifierUserId);
    }

    [Fact]
    public async Task PendingCase_IsAssignedWhenAStudentBecomesAVerifier()
    {
        var (ana, _, attempt) = await AnaFailsAsync();

        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");

        var detail = await DetailAsync(ana, attempt.VerificationCaseId!.Value);
        Assert.Equal("Assigned", detail.Case.Status);
        Assert.Equal(bob.Id, detail.Case.VerifierUserId);
    }

    [Fact]
    public async Task List_ForAStudentWhoIsNotAVerifier_IsEmpty()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await ana.Client.GetAsync(AssessmentFlow.CasesUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await AssessmentFlow.ReadAsync<List<VerificationCaseResource>>(response));
    }

    [Fact]
    public async Task List_AsACoordinator_Returns403()
    {
        var admin = await TestApi.RegisterCoordinatorAsync("admin");

        var response = await admin.Client.GetAsync(AssessmentFlow.CasesUrl);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_WithoutAToken_Returns401()
    {
        var response = await TestApi.CreateClient().GetAsync(AssessmentFlow.CasesUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Detail ----------

    [Fact]
    public async Task Detail_ListsTheFailedQuestionsWithTheChosenAnswerAndNeverTheCorrectOne()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (ana, blueprint, attempt) = await AnaFailsAsync();
        var caseId = attempt.VerificationCaseId!.Value;

        var body = await (await AssessmentFlow.GetCaseAsync(bob, caseId)).Content.ReadAsStringAsync();
        Assert.DoesNotContain("correct", body, StringComparison.OrdinalIgnoreCase);

        var detail = await DetailAsync(bob, caseId);
        Assert.Equal(caseId, detail.Case.Id);
        Assert.Equal(0, detail.Attempt.Score);
        Assert.False(detail.Attempt.Passed);
        Assert.Equal([1, 2, 3, 4, 5], detail.FailedQuestions.Select(q => q.Position));
        Assert.Equal(blueprint.Questions.Select(q => q.Question), detail.FailedQuestions.Select(q => q.Question));
        Assert.Equal(AssessmentFlow.Answers(blueprint, 0), detail.FailedQuestions.Select(q => q.SelectedAnswer));
        Assert.All(detail.FailedQuestions, q => Assert.Equal(4, q.Answers.Count));
        Assert.Equal(ana.Id, detail.Case.StudentId);
    }

    [Fact]
    public async Task Detail_ByTheStudentAndByACoordinator_Returns200()
    {
        var (ana, _, attempt) = await AnaFailsAsync();
        var admin = await TestApi.RegisterCoordinatorAsync("admin");
        var caseId = attempt.VerificationCaseId!.Value;

        Assert.Equal(HttpStatusCode.OK, (await AssessmentFlow.GetCaseAsync(ana, caseId)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AssessmentFlow.GetCaseAsync(admin, caseId)).StatusCode);
    }

    [Fact]
    public async Task Detail_ByAnotherStudent_Returns403()
    {
        var (_, _, attempt) = await AnaFailsAsync();
        var carl = await TestApi.RegisterStudentAsync("carl");

        var response = await AssessmentFlow.GetCaseAsync(carl, attempt.VerificationCaseId!.Value);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NotCaseOwner", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Detail_ForAnUnknownCase_Returns404()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await AssessmentFlow.GetCaseAsync(ana, 9999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("CaseNotFound", await AssessmentFlow.ErrorOf(response));
    }

    // ---------- Evidence (US26) ----------

    [Fact]
    public async Task Evidence_ByTheOwner_Returns200AndIsPersisted()
    {
        var (ana, _, attempt) = await AnaFailsAsync();
        var caseId = attempt.VerificationCaseId!.Value;

        var response = await AssessmentFlow.AttachEvidenceAsync(ana, caseId, "https://github.com/ana/network-lab");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("https://github.com/ana/network-lab",
            (await AssessmentFlow.ReadAsync<VerificationCaseResource>(response)).EvidenceUrl);
        Assert.Equal("https://github.com/ana/network-lab", (await DetailAsync(ana, caseId)).Case.EvidenceUrl);
    }

    [Fact]
    public async Task Evidence_AttachedAgain_ReplacesThePreviousLink()
    {
        var (ana, _, attempt) = await AnaFailsAsync();
        var caseId = attempt.VerificationCaseId!.Value;
        await AssessmentFlow.AttachEvidenceAsync(ana, caseId, "https://github.com/ana/one");

        await AssessmentFlow.AttachEvidenceAsync(ana, caseId, "https://github.com/ana/two");

        Assert.Equal("https://github.com/ana/two", (await DetailAsync(ana, caseId)).Case.EvidenceUrl);
    }

    [Theory]
    [InlineData("not a link")]
    [InlineData("ftp://files.example.com/work")]
    [InlineData("")]
    public async Task Evidence_WithAnInvalidLink_Returns400(string url)
    {
        var (ana, _, attempt) = await AnaFailsAsync();

        var response = await AssessmentFlow.AttachEvidenceAsync(ana, attempt.VerificationCaseId!.Value, url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidEvidenceUrl", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Evidence_ByAnyoneButTheStudent_Returns403()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (_, _, attempt) = await AnaFailsAsync();

        var response = await AssessmentFlow.AttachEvidenceAsync(bob, attempt.VerificationCaseId!.Value,
            "https://github.com/bob/work");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NotCaseOwner", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Evidence_ForAnUnknownCase_Returns404()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await AssessmentFlow.AttachEvidenceAsync(ana, 9999, "https://github.com/ana/work");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("CaseNotFound", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Evidence_ForAResolvedCase_Returns409()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (ana, _, attempt) = await AnaFailsAsync();
        var caseId = attempt.VerificationCaseId!.Value;
        await AssessmentFlow.ResolveAsync(bob, caseId, "Rejected", Notes);

        var response = await AssessmentFlow.AttachEvidenceAsync(ana, caseId, "https://github.com/ana/work");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("CaseAlreadyResolved", await AssessmentFlow.ErrorOf(response));
    }

    // ---------- Resolve (US25) ----------

    [Fact]
    public async Task Resolve_AsApproved_ResolvesTheCaseCompletesTheNodeAndCountsTheReview()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (ana, _, attempt) = await AnaFailsAsync();
        var caseId = attempt.VerificationCaseId!.Value;

        var response = await AssessmentFlow.ResolveAsync(bob, caseId, "Approved", Notes);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resolved = await AssessmentFlow.ReadAsync<VerificationCaseResource>(response);
        Assert.Equal("Resolved", resolved.Status);
        Assert.Equal("Approved", resolved.Decision);
        Assert.Equal(Notes, resolved.RubricNotes);
        Assert.NotNull(resolved.ResolvedAt);

        Assert.Equal("Completed", AssessmentFlow.NodeStatus(await AssessmentFlow.GetPathAsync(ana), Skill));
        var profile = await AssessmentFlow.ReadAsync<VerifierProfileResource>(
            await bob.Client.GetAsync($"{AssessmentFlow.ProfilesUrl}/me"));
        Assert.Equal(1, profile.ReviewCount);
    }

    [Fact]
    public async Task Resolve_AsRejected_KeepsTheNodeAvailable()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (ana, _, attempt) = await AnaFailsAsync();

        var response = await AssessmentFlow.ResolveAsync(bob, attempt.VerificationCaseId!.Value, "Rejected", Notes);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Rejected", (await AssessmentFlow.ReadAsync<VerificationCaseResource>(response)).Decision);
        Assert.Equal("Available", AssessmentFlow.NodeStatus(await AssessmentFlow.GetPathAsync(ana), Skill));
    }

    [Fact]
    public async Task Resolve_AsRejected_LetsTheStudentRetakeAndPass()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (ana, _, attempt) = await AnaFailsAsync();
        await AssessmentFlow.ResolveAsync(bob, attempt.VerificationCaseId!.Value, "Rejected", Notes);

        var path = await AssessmentFlow.GetPathAsync(ana);
        var fresh = await AssessmentFlow.RequestBlueprintAsync(ana, AssessmentFlow.NodeId(path, Skill));
        var second = await AssessmentFlow.SubmitOkAsync(ana, fresh, 5);

        Assert.True(second.Passed);
        Assert.Equal("Completed", AssessmentFlow.NodeStatus(await AssessmentFlow.GetPathAsync(ana), Skill));
    }

    [Fact]
    public async Task Resolve_AsRejected_DoesNotAllowAnsweringTheSameAssessmentAgain()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (ana, blueprint, attempt) = await AnaFailsAsync();
        await AssessmentFlow.ResolveAsync(bob, attempt.VerificationCaseId!.Value, "Rejected", Notes);

        var response = await AssessmentFlow.SubmitAsync(ana, blueprint.Id, AssessmentFlow.Answers(blueprint, 5));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("AttemptAlreadySubmitted", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Resolve_ByAVerifierWhoIsNotAssigned_Returns403()
    {
        await AssessmentFlow.RegisterVerifierAsync("bob");
        var carl = await AssessmentFlow.RegisterVerifierAsync("carl");
        var (_, _, attempt) = await AnaFailsAsync();

        var response = await AssessmentFlow.ResolveAsync(carl, attempt.VerificationCaseId!.Value, "Approved", Notes);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NotAssignedVerifier", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Resolve_ByTheStudentOfTheCase_Returns403()
    {
        await AssessmentFlow.RegisterVerifierAsync("bob");
        var (ana, _, attempt) = await AnaFailsAsync();

        var response = await AssessmentFlow.ResolveAsync(ana, attempt.VerificationCaseId!.Value, "Approved", Notes);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NotAssignedVerifier", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Resolve_AsACoordinator_Returns403()
    {
        await AssessmentFlow.RegisterVerifierAsync("bob");
        var (_, _, attempt) = await AnaFailsAsync();
        var admin = await TestApi.RegisterCoordinatorAsync("admin");

        var response = await AssessmentFlow.ResolveAsync(admin, attempt.VerificationCaseId!.Value, "Approved", Notes);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Resolve_ForAnUnknownCase_Returns404()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");

        var response = await AssessmentFlow.ResolveAsync(bob, 9999, "Approved", Notes);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("CaseNotFound", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Resolve_ACaseThatIsAlreadyResolved_Returns409()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (_, _, attempt) = await AnaFailsAsync();
        var caseId = attempt.VerificationCaseId!.Value;
        await AssessmentFlow.ResolveAsync(bob, caseId, "Approved", Notes);

        var response = await AssessmentFlow.ResolveAsync(bob, caseId, "Rejected", Notes);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("CaseAlreadyResolved", await AssessmentFlow.ErrorOf(response));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Resolve_WithoutNotes_Returns400(string? notes)
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (_, _, attempt) = await AnaFailsAsync();

        var response = await AssessmentFlow.ResolveAsync(bob, attempt.VerificationCaseId!.Value, "Approved", notes);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("RubricNotesRequired", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Resolve_WithTooLongNotes_Returns400()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (_, _, attempt) = await AnaFailsAsync();

        var response = await AssessmentFlow.ResolveAsync(bob, attempt.VerificationCaseId!.Value, "Approved",
            new string('a', 2001));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("RubricNotesTooLong", await AssessmentFlow.ErrorOf(response));
    }

    [Theory]
    [InlineData("Maybe")]
    [InlineData("")]
    [InlineData("99")]
    [InlineData(null)]
    public async Task Resolve_WithAnInvalidDecision_Returns400(string? decision)
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (_, _, attempt) = await AnaFailsAsync();

        var response = await AssessmentFlow.ResolveAsync(bob, attempt.VerificationCaseId!.Value, decision, Notes);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidDecision", await AssessmentFlow.ErrorOf(response));
    }
}