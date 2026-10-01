using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SkillSwap.Platform.AssessmentPeerReview.Application.Internal.CommandServices;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Events;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.LearningPathEngine.Application.ACL;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Resources.Errors;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Application;

public class VerificationCaseCommandServiceTests
{
    private const int StudentId = 1;
    private const int VerifierId = 2;
    private const string EvidenceUrl = "https://github.com/student/project";

    private readonly FakeVerificationCaseRepository _cases = new();
    private readonly FakeDomainEventPublisher _events = new();
    private readonly FakeLearningPathContextFacade _learningPath = new();
    private readonly FakeVerifierProfileRepository _profiles = new();
    private readonly VerificationCaseCommandService _service;
    private readonly FakeUnitOfWork _unitOfWork = new();

    public VerificationCaseCommandServiceTests()
    {
        _service = new VerificationCaseCommandService(
            _cases,
            _profiles,
            _learningPath,
            _events,
            _unitOfWork,
            new FakeLocalizer<ErrorMessage>(),
            NullLogger<VerificationCaseCommandService>.Instance);
    }

    private async Task<VerificationCase> AddAssignedCaseAsync()
    {
        var verificationCase = new VerificationCase(1, StudentId, 10, "http-basics").AssignVerifier(VerifierId);
        await _cases.AddAsync(verificationCase);
        return verificationCase;
    }

    private Task<VerifierProfile> AddVerifierAsync(int userId = VerifierId)
    {
        var profile = new VerifierProfile(userId, "http-basics");
        return _profiles.AddAsync(profile).ContinueWith(_ => profile);
    }

    private Task<Result<VerificationCase>> Attach(int caseId, int studentId = StudentId, string url = EvidenceUrl)
    {
        return _service.Handle(new AttachCaseEvidenceCommand(caseId, studentId, url), CancellationToken.None);
    }

    private Task<Result<VerificationCase>> Resolve(int caseId, int verifierId = VerifierId,
        ReviewDecision decision = ReviewDecision.Approved, string notes = "Meets the rubric.")
    {
        return _service.Handle(new ResolveVerificationCaseCommand(caseId, verifierId, decision, notes),
            CancellationToken.None);
    }

    private static void AssertFailure(Result<VerificationCase> result, AssessmentPeerReviewError expected)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expected, Assert.IsType<AssessmentPeerReviewError>(result.Error));
    }

    // ---------- Attach evidence ----------

    [Fact]
    public async Task Attach_ByTheOwner_StoresTheEvidence()
    {
        var verificationCase = await AddAssignedCaseAsync();

        var result = await Attach(verificationCase.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(EvidenceUrl, result.Value!.EvidenceUrl);
    }

    [Fact]
    public async Task Attach_ToAPendingCase_IsAllowed()
    {
        var pending = new VerificationCase(1, StudentId, 10, "http-basics");
        await _cases.AddAsync(pending);

        Assert.True((await Attach(pending.Id)).IsSuccess);
    }

    [Fact]
    public async Task Attach_ToAnUnknownCase_FailsWithCaseNotFound()
    {
        AssertFailure(await Attach(99), AssessmentPeerReviewError.CaseNotFound);
    }

    [Fact]
    public async Task Attach_ByAnotherStudent_FailsWithNotCaseOwner()
    {
        var verificationCase = await AddAssignedCaseAsync();

        AssertFailure(await Attach(verificationCase.Id, studentId: 7), AssessmentPeerReviewError.NotCaseOwner);
        Assert.Null(verificationCase.EvidenceUrl);
    }

    [Fact]
    public async Task Attach_ToAResolvedCase_FailsWithCaseAlreadyResolved()
    {
        var verificationCase = await AddAssignedCaseAsync();
        verificationCase.Resolve(ReviewDecision.Rejected, "Needs work.");

        AssertFailure(await Attach(verificationCase.Id), AssessmentPeerReviewError.CaseAlreadyResolved);
    }

    [Fact]
    public async Task Attach_WithAnInvalidLink_FailsWithInvalidEvidenceUrl()
    {
        var verificationCase = await AddAssignedCaseAsync();

        AssertFailure(await Attach(verificationCase.Id, url: "not a link"),
            AssessmentPeerReviewError.InvalidEvidenceUrl);
        Assert.Null(verificationCase.EvidenceUrl);
    }

    [Fact]
    public async Task Attach_WhenPersistenceFails_FailsWithDatabaseError()
    {
        var verificationCase = await AddAssignedCaseAsync();
        _unitOfWork.ExceptionToThrow = new DbUpdateException("failure");

        AssertFailure(await Attach(verificationCase.Id), AssessmentPeerReviewError.DatabaseError);
    }

    // ---------- Resolve ----------

    [Fact]
    public async Task Resolve_AsApproved_ResolvesTheCaseCompletesTheNodeAndPublishesTheEvent()
    {
        var verificationCase = await AddAssignedCaseAsync();
        var profile = await AddVerifierAsync();

        var result = await Resolve(verificationCase.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(CaseStatus.Resolved, result.Value!.Status);
        Assert.Equal(ReviewDecision.Approved, result.Value.Decision);
        Assert.Equal("Meets the rubric.", result.Value.RubricNotes);
        Assert.Equal([10], _learningPath.CompletedNodes);
        Assert.Equal(1, profile.ReviewCount);
        var published = Assert.IsType<VerificationCaseResolved>(Assert.Single(_events.Published));
        Assert.Equal(verificationCase.Id, published.CaseId);
        Assert.Equal(StudentId, published.StudentId);
        Assert.Equal(VerifierId, published.VerifierUserId);
        Assert.Equal(ReviewDecision.Approved, published.Decision);
    }

    [Fact]
    public async Task Resolve_AsRejected_DoesNotCompleteTheNodeButStillPublishesTheEvent()
    {
        var verificationCase = await AddAssignedCaseAsync();
        var profile = await AddVerifierAsync();

        var result = await Resolve(verificationCase.Id, decision: ReviewDecision.Rejected, notes: "Missing tests.");

        Assert.True(result.IsSuccess);
        Assert.Equal(ReviewDecision.Rejected, result.Value!.Decision);
        Assert.Empty(_learningPath.CompletedNodes);
        Assert.Equal(1, profile.ReviewCount);
        Assert.Equal(ReviewDecision.Rejected,
            Assert.IsType<VerificationCaseResolved>(Assert.Single(_events.Published)).Decision);
    }

    [Fact]
    public async Task Resolve_AnApprovedCaseWhoseNodeIsAlreadyCompleted_StillSucceeds()
    {
        var verificationCase = await AddAssignedCaseAsync();
        await AddVerifierAsync();
        _learningPath.NextOutcome = NodeCompletionOutcome.AlreadyCompleted;

        Assert.True((await Resolve(verificationCase.Id)).IsSuccess);
    }

    [Fact]
    public async Task Resolve_AnUnknownCase_FailsWithCaseNotFound()
    {
        await AddVerifierAsync();

        AssertFailure(await Resolve(99), AssessmentPeerReviewError.CaseNotFound);
    }

    [Fact]
    public async Task Resolve_ByAVerifierWhoIsNotAssigned_FailsWithNotAssignedVerifier()
    {
        var verificationCase = await AddAssignedCaseAsync();
        await AddVerifierAsync(3);

        AssertFailure(await Resolve(verificationCase.Id, verifierId: 3), AssessmentPeerReviewError.NotAssignedVerifier);
        Assert.Equal(CaseStatus.Assigned, verificationCase.Status);
    }

    [Fact]
    public async Task Resolve_APendingCase_FailsWithNotAssignedVerifier()
    {
        var pending = new VerificationCase(1, StudentId, 10, "http-basics");
        await _cases.AddAsync(pending);
        await AddVerifierAsync();

        AssertFailure(await Resolve(pending.Id), AssessmentPeerReviewError.NotAssignedVerifier);
    }

    [Fact]
    public async Task Resolve_AResolvedCase_FailsWithCaseAlreadyResolved()
    {
        var verificationCase = await AddAssignedCaseAsync();
        await AddVerifierAsync();
        await Resolve(verificationCase.Id);

        AssertFailure(await Resolve(verificationCase.Id), AssessmentPeerReviewError.CaseAlreadyResolved);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Resolve_WithoutNotes_FailsWithRubricNotesRequired(string notes)
    {
        var verificationCase = await AddAssignedCaseAsync();
        await AddVerifierAsync();

        AssertFailure(await Resolve(verificationCase.Id, notes: notes), AssessmentPeerReviewError.RubricNotesRequired);
        Assert.Equal(CaseStatus.Assigned, verificationCase.Status);
    }

    [Fact]
    public async Task Resolve_WithTooLongNotes_FailsWithRubricNotesTooLong()
    {
        var verificationCase = await AddAssignedCaseAsync();
        await AddVerifierAsync();
        var notes = new string('a', VerificationCase.MaxRubricNotesLength + 1);

        AssertFailure(await Resolve(verificationCase.Id, notes: notes), AssessmentPeerReviewError.RubricNotesTooLong);
    }

    [Fact]
    public async Task Resolve_WithAnUndefinedDecision_FailsWithInvalidDecision()
    {
        var verificationCase = await AddAssignedCaseAsync();
        await AddVerifierAsync();

        AssertFailure(await Resolve(verificationCase.Id, decision: (ReviewDecision)99),
            AssessmentPeerReviewError.InvalidDecision);
    }

    [Fact]
    public async Task Resolve_WithoutAVerifierProfile_FailsWithNotAVerifier()
    {
        var verificationCase = await AddAssignedCaseAsync();

        AssertFailure(await Resolve(verificationCase.Id), AssessmentPeerReviewError.NotAVerifier);
    }

    [Fact]
    public async Task Resolve_WithARevokedProfile_FailsWithNotAVerifier()
    {
        var verificationCase = await AddAssignedCaseAsync();
        (await AddVerifierAsync()).Revoke();

        AssertFailure(await Resolve(verificationCase.Id), AssessmentPeerReviewError.NotAVerifier);
    }

    [Fact]
    public async Task Resolve_WhenTheNodeCannotBeCompleted_FailsAndPublishesNothing()
    {
        var verificationCase = await AddAssignedCaseAsync();
        await AddVerifierAsync();
        _learningPath.NextOutcome = NodeCompletionOutcome.NodeNotFound;

        AssertFailure(await Resolve(verificationCase.Id), AssessmentPeerReviewError.NodeNotAvailable);
        Assert.Empty(_events.Published);
    }

    [Fact]
    public async Task Resolve_WhenPersistenceFails_FailsWithDatabaseError()
    {
        var verificationCase = await AddAssignedCaseAsync();
        await AddVerifierAsync();
        _unitOfWork.ExceptionToThrow = new DbUpdateException("failure");

        AssertFailure(await Resolve(verificationCase.Id), AssessmentPeerReviewError.DatabaseError);
        Assert.Empty(_events.Published);
    }
}