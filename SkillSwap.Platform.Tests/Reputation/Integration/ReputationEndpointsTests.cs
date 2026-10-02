using System.Net;
using System.Net.Http.Json;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;
using SkillSwap.Platform.Reputation.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Reputation.Integration;

public class ReputationEndpointsTests : ApiTestBase
{
    private const string Skill = "networking-basics";
    private const string Notes = "Solid understanding of the network layers.";
    private const string EmployabilityUrl = "/api/v1/student-employability-scores";
    private const string ReliabilityUrl = "/api/v1/verifier-reliabilities";

    private static Task<HttpResponseMessage> EmployabilityAsync(SignedInUser actor, int studentId)
    {
        return actor.Client.GetAsync($"{EmployabilityUrl}/{studentId}");
    }

    private static Task<HttpResponseMessage> ReliabilityAsync(SignedInUser actor, int verifierUserId)
    {
        return actor.Client.GetAsync($"{ReliabilityUrl}/{verifierUserId}");
    }

    private static async Task<StudentEmployabilityResource> ReadEmployabilityAsync(SignedInUser actor,
        int studentId)
    {
        var response = await EmployabilityAsync(actor, studentId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await AssessmentFlow.ReadAsync<StudentEmployabilityResource>(response);
    }

    private static async Task<VerifierReliabilityResource> ReadReliabilityAsync(SignedInUser actor,
        int verifierUserId)
    {
        var response = await ReliabilityAsync(actor, verifierUserId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await AssessmentFlow.ReadAsync<VerifierReliabilityResource>(response);
    }

    /// <summary>
    ///     A student with a path who fails the assessment, which opens a case for the verifier.
    /// </summary>
    private static async Task<(SignedInUser Student, int CaseId)> StudentFailsAsync(string username)
    {
        var student = await TestApi.RegisterStudentAsync(username);
        await AssessmentFlow.DeclareGoalAsync(student);
        var (_, attempt) = await AssessmentFlow.FailSkillAsync(student, Skill);
        return (student, attempt.VerificationCaseId!.Value);
    }

    private static async Task ResolveOkAsync(SignedInUser verifier, int caseId, string decision)
    {
        var response = await AssessmentFlow.ResolveAsync(verifier, caseId, decision, Notes);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- Employability: automatic approval ----------

    [Fact]
    public async Task Employability_AfterPassingAnAssessment_Returns200WithTenPoints()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        await AssessmentFlow.DeclareGoalAsync(ana);
        await AssessmentFlow.CompleteSkillAsync(ana, Skill);

        var employability = await ReadEmployabilityAsync(ana, ana.Id);

        Assert.Equal(ana.Id, employability.StudentId);
        Assert.Equal(1, employability.VerifiedSkillsCount);
        Assert.Equal(10, employability.Score);
        Assert.Equal(DateTimeKind.Utc, employability.UpdatedAt.Kind);
    }

    [Fact]
    public async Task Employability_WithTwoCertifiedSkills_AddsUp()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        await AssessmentFlow.DeclareGoalAsync(ana);
        await AssessmentFlow.CompleteSkillAsync(ana, Skill);
        await AssessmentFlow.CompleteSkillAsync(ana, "http-basics");

        var employability = await ReadEmployabilityAsync(ana, ana.Id);

        Assert.Equal(2, employability.VerifiedSkillsCount);
        Assert.Equal(20, employability.Score);
    }

    [Fact]
    public async Task Employability_AfterAFailedAttempt_Returns404()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        await AssessmentFlow.DeclareGoalAsync(ana);
        await AssessmentFlow.FailSkillAsync(ana, Skill);

        var response = await EmployabilityAsync(ana, ana.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("ReputationNotFound", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Employability_WithoutAnyActivity_Returns404()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await EmployabilityAsync(ana, ana.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("ReputationNotFound", await AssessmentFlow.ErrorOf(response));
    }

    // ---------- Employability: verifier decisions ----------

    [Fact]
    public async Task Employability_AfterAVerifierApproves_CountsTheSkillOnce()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (ana, caseId) = await StudentFailsAsync("ana");

        await ResolveOkAsync(bob, caseId, "Approved");

        var employability = await ReadEmployabilityAsync(ana, ana.Id);
        Assert.Equal(1, employability.VerifiedSkillsCount);
        Assert.Equal(10, employability.Score);
    }

    [Fact]
    public async Task Employability_AfterAVerifierRejects_DoesNotCountTheSkill()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (ana, caseId) = await StudentFailsAsync("ana");

        await ResolveOkAsync(bob, caseId, "Rejected");

        Assert.Equal(HttpStatusCode.NotFound, (await EmployabilityAsync(ana, ana.Id)).StatusCode);
    }

    [Fact]
    public async Task Employability_AfterARejectionAndAPassingRetake_CountsOnlyThePass()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (ana, caseId) = await StudentFailsAsync("ana");
        await ResolveOkAsync(bob, caseId, "Rejected");

        var path = await AssessmentFlow.GetPathAsync(ana);
        var fresh = await AssessmentFlow.RequestBlueprintAsync(ana, AssessmentFlow.NodeId(path, Skill));
        await AssessmentFlow.SubmitOkAsync(ana, fresh, 5);

        var employability = await ReadEmployabilityAsync(ana, ana.Id);
        Assert.Equal(1, employability.VerifiedSkillsCount);
        Assert.Equal(10, employability.Score);
    }

    [Fact]
    public async Task Employability_OfAVerifier_IsNotAffectedByTheCasesTheyResolve()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (_, caseId) = await StudentFailsAsync("ana");

        await ResolveOkAsync(bob, caseId, "Approved");

        var employability = await ReadEmployabilityAsync(bob, bob.Id);
        Assert.Equal(1, employability.VerifiedSkillsCount);
        Assert.Equal(10, employability.Score);
    }

    // ---------- Employability: access ----------

    [Fact]
    public async Task Employability_ByAnotherStudent_Returns403()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        await AssessmentFlow.DeclareGoalAsync(ana);
        await AssessmentFlow.CompleteSkillAsync(ana, Skill);
        var bob = await TestApi.RegisterStudentAsync("bob");

        var response = await EmployabilityAsync(bob, ana.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NotReputationOwner", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Employability_ByACoordinator_Returns200()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        await AssessmentFlow.DeclareGoalAsync(ana);
        await AssessmentFlow.CompleteSkillAsync(ana, Skill);
        var admin = await TestApi.RegisterCoordinatorAsync("admin");

        var employability = await ReadEmployabilityAsync(admin, ana.Id);

        Assert.Equal(1, employability.VerifiedSkillsCount);
    }

    [Fact]
    public async Task Employability_OfAnUnknownStudent_ReadByACoordinator_Returns404()
    {
        var admin = await TestApi.RegisterCoordinatorAsync("admin");

        var response = await EmployabilityAsync(admin, 9999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Employability_WithoutAToken_Returns401()
    {
        var response = await TestApi.CreateClient().GetAsync($"{EmployabilityUrl}/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Reliability ----------

    [Fact]
    public async Task Reliability_AfterApprovingACase_Returns200WithTheCounters()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (_, caseId) = await StudentFailsAsync("ana");

        await ResolveOkAsync(bob, caseId, "Approved");

        var reliability = await ReadReliabilityAsync(bob, bob.Id);
        Assert.Equal(bob.Id, reliability.VerifierUserId);
        Assert.Equal(1, reliability.ResolvedCasesCount);
        Assert.Equal(0, reliability.OverturnedDecisionsCount);
        Assert.Equal(0, reliability.SanctionsCount);
        Assert.Equal(100, reliability.Score);
        Assert.Equal(DateTimeKind.Utc, reliability.UpdatedAt.Kind);
    }

    [Fact]
    public async Task Reliability_AfterRejectingACase_AlsoCountsIt()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (_, caseId) = await StudentFailsAsync("ana");

        await ResolveOkAsync(bob, caseId, "Rejected");

        Assert.Equal(1, (await ReadReliabilityAsync(bob, bob.Id)).ResolvedCasesCount);
    }

    [Fact]
    public async Task Reliability_AfterSeveralCases_Accumulates()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (_, anaCase) = await StudentFailsAsync("ana");
        var (_, carlCase) = await StudentFailsAsync("carl");

        await ResolveOkAsync(bob, anaCase, "Approved");
        await ResolveOkAsync(bob, carlCase, "Rejected");

        var reliability = await ReadReliabilityAsync(bob, bob.Id);
        Assert.Equal(2, reliability.ResolvedCasesCount);
        Assert.Equal(100, reliability.Score);
    }

    [Fact]
    public async Task Reliability_UpdatesTheRatingOfTheVerifierProfile()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (_, caseId) = await StudentFailsAsync("ana");

        await ResolveOkAsync(bob, caseId, "Approved");

        var profile = await AssessmentFlow.ReadAsync<VerifierProfileResource>(
            await bob.Client.GetAsync($"{AssessmentFlow.ProfilesUrl}/me"));
        Assert.Equal(100, profile.Rating);
        Assert.Equal(1, profile.ReviewCount);
    }

    [Fact]
    public async Task Reliability_OfAVerifierWhoResolvedNothing_Returns404()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");

        var response = await ReliabilityAsync(bob, bob.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("ReputationNotFound", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Reliability_ByAnotherUser_Returns403()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (ana, caseId) = await StudentFailsAsync("ana");
        await ResolveOkAsync(bob, caseId, "Approved");

        var response = await ReliabilityAsync(ana, bob.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NotReputationOwner", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Reliability_ByACoordinator_Returns200()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var (_, caseId) = await StudentFailsAsync("ana");
        await ResolveOkAsync(bob, caseId, "Approved");
        var admin = await TestApi.RegisterCoordinatorAsync("admin");

        var reliability = await ReadReliabilityAsync(admin, bob.Id);

        Assert.Equal(1, reliability.ResolvedCasesCount);
    }

    [Fact]
    public async Task Reliability_WithoutAToken_Returns401()
    {
        var response = await TestApi.CreateClient().GetAsync($"{ReliabilityUrl}/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}