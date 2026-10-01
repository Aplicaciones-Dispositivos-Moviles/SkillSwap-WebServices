using SkillSwap.Platform.AssessmentPeerReview.Application.Internal;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Services;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Application;

public class CaseAssignmentServiceTests
{
    private const string Skill = "http-basics";

    private readonly FakeVerificationCaseRepository _cases = new();
    private readonly FakeVerifierProfileRepository _profiles = new();
    private readonly CaseAssignmentService _service;

    public CaseAssignmentServiceTests()
    {
        _service = new CaseAssignmentService(_profiles, _cases, new VerifierMatcher(), new FakeUnitOfWork());
    }

    private async Task AddVerifierAsync(int userId, string skill = Skill, bool available = true)
    {
        await _profiles.AddAsync(new VerifierProfile(userId, skill).SetAvailability(available));
    }

    private async Task<VerificationCase> AddCaseAsync(int studentId = 1, string skill = Skill)
    {
        var verificationCase = new VerificationCase(1, studentId, 10, skill);
        await _cases.AddAsync(verificationCase);
        return verificationCase;
    }

    [Fact]
    public async Task TryAssign_AssignsTheCaseToAnAvailableVerifierOfTheSkill()
    {
        await AddVerifierAsync(2);
        var verificationCase = await AddCaseAsync();

        var assigned = await _service.TryAssignAsync(verificationCase, CancellationToken.None);

        Assert.True(assigned);
        Assert.Equal(CaseStatus.Assigned, verificationCase.Status);
        Assert.Equal(2, verificationCase.VerifierUserId);
    }

    [Fact]
    public async Task TryAssign_ChoosesTheVerifierWithTheFewestOpenCases()
    {
        await AddVerifierAsync(2);
        await AddVerifierAsync(3);
        var busy = await AddCaseAsync(studentId: 7);
        busy.AssignVerifier(2);
        var verificationCase = await AddCaseAsync();

        await _service.TryAssignAsync(verificationCase, CancellationToken.None);

        Assert.Equal(3, verificationCase.VerifierUserId);
    }

    [Fact]
    public async Task TryAssign_WithoutQualifiedVerifiers_LeavesTheCasePending()
    {
        await AddVerifierAsync(2, skill: "sql-fundamentals");
        await AddVerifierAsync(3, available: false);
        var verificationCase = await AddCaseAsync();

        var assigned = await _service.TryAssignAsync(verificationCase, CancellationToken.None);

        Assert.False(assigned);
        Assert.Equal(CaseStatus.Pending, verificationCase.Status);
    }

    [Fact]
    public async Task TryAssign_NeverAssignsTheCaseToItsOwnStudent()
    {
        await AddVerifierAsync(1);
        var verificationCase = await AddCaseAsync(studentId: 1);

        Assert.False(await _service.TryAssignAsync(verificationCase, CancellationToken.None));
    }

    [Fact]
    public async Task AssignPending_AssignsTheOldestCasesFirstSpreadingTheWorkload()
    {
        await AddVerifierAsync(2);
        await AddVerifierAsync(3);
        var first = await AddCaseAsync(studentId: 7);
        var second = await AddCaseAsync(studentId: 8);

        var assigned = await _service.AssignPendingAsync([Skill], CancellationToken.None);

        Assert.Equal(2, assigned);
        Assert.Equal(2, first.VerifierUserId);
        Assert.Equal(3, second.VerifierUserId);
    }

    [Fact]
    public async Task AssignPending_OnlyTouchesTheRequestedSkills()
    {
        await AddVerifierAsync(2);
        var other = await AddCaseAsync(skill: "sql-fundamentals");

        var assigned = await _service.AssignPendingAsync([Skill], CancellationToken.None);

        Assert.Equal(0, assigned);
        Assert.Equal(CaseStatus.Pending, other.Status);
    }

    [Fact]
    public async Task AssignPending_WithoutPendingCases_AssignsNothing()
    {
        await AddVerifierAsync(2);

        Assert.Equal(0, await _service.AssignPendingAsync([Skill], CancellationToken.None));
    }
}