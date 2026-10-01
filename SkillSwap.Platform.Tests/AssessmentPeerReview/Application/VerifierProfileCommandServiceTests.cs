using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SkillSwap.Platform.AssessmentPeerReview.Application.Internal;
using SkillSwap.Platform.AssessmentPeerReview.Application.Internal.CommandServices;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Services;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Resources.Errors;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Application;

public class VerifierProfileCommandServiceTests
{
    private const string Skill = "http-basics";

    private readonly FakeVerificationCaseRepository _cases = new();
    private readonly FakeLearningPathContextFacade _learningPath = new();
    private readonly FakeVerifierProfileRepository _profiles = new();
    private readonly VerifierProfileCommandService _service;
    private readonly FakeUnitOfWork _unitOfWork = new();

    public VerifierProfileCommandServiceTests()
    {
        _learningPath.CompletedSkills.Add((2, Skill));
        _service = new VerifierProfileCommandService(
            _profiles,
            _learningPath,
            new CaseAssignmentService(_profiles, _cases, new VerifierMatcher(), _unitOfWork),
            _unitOfWork,
            new FakeLocalizer<ErrorMessage>(),
            NullLogger<VerifierProfileCommandService>.Instance);
    }

    private Task<Result<VerifierProfile>> Create(string skill = Skill, int userId = 2)
    {
        return _service.Handle(new CreateVerifierProfileCommand(userId, skill), CancellationToken.None);
    }

    private Task<Result<VerifierProfile>> SetAvailability(bool available, int userId = 2)
    {
        return _service.Handle(new UpdateVerifierAvailabilityCommand(userId, available), CancellationToken.None);
    }

    private static void AssertFailure(Result<VerifierProfile> result, AssessmentPeerReviewError expected)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expected, Assert.IsType<AssessmentPeerReviewError>(result.Error));
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_ForACompletedSkill_CreatesAnAvailableProfile()
    {
        var result = await Create();

        Assert.True(result.IsSuccess);
        var profile = Assert.Single(_profiles.Profiles);
        Assert.Same(profile, result.Value);
        Assert.Equal(2, profile.VerifierUserId);
        Assert.Equal([Skill], profile.SkillTags);
        Assert.True(profile.Available);
        Assert.True(profile.Verified);
    }

    [Fact]
    public async Task Create_ForAnotherCompletedSkill_AddsItToTheExistingProfile()
    {
        _learningPath.CompletedSkills.Add((2, "rest-api-design"));
        await Create();

        var result = await Create("rest-api-design");

        Assert.True(result.IsSuccess);
        Assert.Single(_profiles.Profiles);
        Assert.Equal(["http-basics", "rest-api-design"], result.Value!.SkillTags);
    }

    [Fact]
    public async Task Create_ForASkillAlreadyEnabled_FailsWithVerifierSkillAlreadyEnabled()
    {
        await Create();

        AssertFailure(await Create(), AssessmentPeerReviewError.VerifierSkillAlreadyEnabled);
    }

    [Fact]
    public async Task Create_ForASkillThatWasNotCompleted_FailsWithSkillNotCompleted()
    {
        AssertFailure(await Create("sql-fundamentals"), AssessmentPeerReviewError.SkillNotCompleted);
        Assert.Empty(_profiles.Profiles);
    }

    [Fact]
    public async Task Create_ForAnotherStudentsCompletedSkill_FailsWithSkillNotCompleted()
    {
        AssertFailure(await Create(userId: 3), AssessmentPeerReviewError.SkillNotCompleted);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithABlankSkill_FailsWithInvalidSkillTag(string skill)
    {
        AssertFailure(await Create(skill), AssessmentPeerReviewError.InvalidSkillTag);
    }

    [Fact]
    public async Task Create_WithARevokedProfile_FailsWithNotAVerifier()
    {
        await _profiles.AddAsync(new VerifierProfile(2, "rest-api-design").Revoke());

        AssertFailure(await Create(), AssessmentPeerReviewError.NotAVerifier);
    }

    [Fact]
    public async Task Create_AssignsThePendingCasesOfTheSkill()
    {
        var pending = new VerificationCase(1, 1, 10, Skill);
        var other = new VerificationCase(2, 1, 11, "sql-fundamentals");
        await _cases.AddAsync(pending);
        await _cases.AddAsync(other);

        await Create();

        Assert.Equal(CaseStatus.Assigned, pending.Status);
        Assert.Equal(2, pending.VerifierUserId);
        Assert.Equal(CaseStatus.Pending, other.Status);
    }

    [Fact]
    public async Task Create_WhenPersistenceFails_FailsWithDatabaseError()
    {
        _unitOfWork.ExceptionToThrow = new DbUpdateException("failure");

        AssertFailure(await Create(), AssessmentPeerReviewError.DatabaseError);
    }

    // ---------- Availability ----------

    [Fact]
    public async Task SetAvailability_SwitchesItOff()
    {
        await Create();

        var result = await SetAvailability(false);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Available);
    }

    [Fact]
    public async Task SetAvailability_SwitchedOn_AssignsThePendingCases()
    {
        await _profiles.AddAsync(new VerifierProfile(2, Skill).SetAvailability(false));
        var pending = new VerificationCase(1, 1, 10, Skill);
        await _cases.AddAsync(pending);

        var result = await SetAvailability(true);

        Assert.True(result.Value!.Available);
        Assert.Equal(CaseStatus.Assigned, pending.Status);
        Assert.Equal(2, pending.VerifierUserId);
    }

    [Fact]
    public async Task SetAvailability_SwitchedOff_KeepsTheCasesAlreadyAssigned()
    {
        await Create();
        var assigned = new VerificationCase(1, 1, 10, Skill).AssignVerifier(2);
        await _cases.AddAsync(assigned);

        await SetAvailability(false);

        Assert.Equal(CaseStatus.Assigned, assigned.Status);
        Assert.Equal(2, assigned.VerifierUserId);
    }

    [Fact]
    public async Task SetAvailability_WithoutAProfile_FailsWithNotAVerifier()
    {
        AssertFailure(await SetAvailability(true), AssessmentPeerReviewError.NotAVerifier);
    }

    [Fact]
    public async Task SetAvailability_WithARevokedProfile_FailsWithNotAVerifier()
    {
        await _profiles.AddAsync(new VerifierProfile(2, Skill).Revoke());

        AssertFailure(await SetAvailability(true), AssessmentPeerReviewError.NotAVerifier);
    }
}