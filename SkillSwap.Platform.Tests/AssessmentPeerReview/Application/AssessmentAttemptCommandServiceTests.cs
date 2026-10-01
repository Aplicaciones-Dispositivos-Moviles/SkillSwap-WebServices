using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SkillSwap.Platform.AssessmentPeerReview.Application.CommandServices;
using SkillSwap.Platform.AssessmentPeerReview.Application.Internal;
using SkillSwap.Platform.AssessmentPeerReview.Application.Internal.CommandServices;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Events;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Services;
using SkillSwap.Platform.LearningPathEngine.Application.ACL;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Resources.Errors;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Application;

public class AssessmentAttemptCommandServiceTests
{
    private static readonly int[] Passing = [1, 2, 3, 0, 1];
    private static readonly int[] Failing = [0, 0, 0, 1, 0];

    private readonly FakeAssessmentAttemptRepository _attempts = new();
    private readonly FakeVerificationCaseRepository _cases = new();
    private readonly FakeDomainEventPublisher _events = new();
    private readonly FakeLearningPathContextFacade _learningPath = new();
    private readonly FakeVerifierProfileRepository _profiles = new();
    private readonly AssessmentAttemptCommandService _service;
    private readonly FakeUnitOfWork _unitOfWork = new();

    public AssessmentAttemptCommandServiceTests()
    {
        _learningPath.AddBlueprint();
        _service = new AssessmentAttemptCommandService(
            _attempts,
            _cases,
            _learningPath,
            new CaseAssignmentService(_profiles, _cases, new VerifierMatcher(), _unitOfWork),
            _events,
            _unitOfWork,
            new FakeLocalizer<ErrorMessage>(),
            NullLogger<AssessmentAttemptCommandService>.Instance);
    }

    private Task<Result<SubmitAssessmentAttemptOutcome>> Submit(int[]? answers = null, int blueprintId = 1,
        int studentId = 1)
    {
        return _service.Handle(new SubmitAssessmentAttemptCommand(studentId, blueprintId, answers ?? Passing),
            CancellationToken.None);
    }

    private static void AssertFailure(Result<SubmitAssessmentAttemptOutcome> result,
        AssessmentPeerReviewError expected)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expected, Assert.IsType<AssessmentPeerReviewError>(result.Error));
    }

    // ---------- Approved attempt ----------

    [Fact]
    public async Task Submit_WithPassingAnswers_PassesCompletesTheNodeAndPublishesTheEvent()
    {
        var result = await Submit();

        Assert.True(result.IsSuccess);
        var attempt = result.Value!.Attempt;
        Assert.True(attempt.Passed);
        Assert.Equal(5, attempt.Score.Value);
        Assert.Null(result.Value.VerificationCase);
        Assert.Equal([10], _learningPath.CompletedNodes);
        Assert.Empty(_cases.Cases);
        var published = Assert.IsType<AssessmentAttemptPassed>(Assert.Single(_events.Published));
        Assert.Equal(attempt.Id, published.AttemptId);
        Assert.Equal(10, published.PathNodeId);
        Assert.Equal("http-basics", published.SkillTag);
    }

    [Fact]
    public async Task Submit_WithExactlyTheThreshold_Passes()
    {
        var result = await Submit([1, 2, 3, 0, 3]);

        Assert.True(result.Value!.Attempt.Passed);
        Assert.Equal(4, result.Value.Attempt.Score.Value);
    }

    // ---------- Failed attempt ----------

    [Fact]
    public async Task Submit_WithFailingAnswers_OpensAndAssignsACase()
    {
        await _profiles.AddAsync(new VerifierProfile(2, "http-basics"));

        var result = await Submit(Failing);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Attempt.Passed);
        var verificationCase = result.Value.VerificationCase!;
        Assert.Same(verificationCase, Assert.Single(_cases.Cases));
        Assert.Equal(result.Value.Attempt.Id, verificationCase.AttemptId);
        Assert.Equal(CaseStatus.Assigned, verificationCase.Status);
        Assert.Equal(2, verificationCase.VerifierUserId);
        Assert.Equal(10, verificationCase.PathNodeId);
        Assert.Empty(_learningPath.CompletedNodes);
        Assert.Empty(_events.Published);
    }

    [Fact]
    public async Task Submit_WithFailingAnswersAndNoVerifiers_LeavesTheCasePending()
    {
        var result = await Submit(Failing);

        Assert.True(result.IsSuccess);
        Assert.Equal(CaseStatus.Pending, result.Value!.VerificationCase!.Status);
        Assert.Null(result.Value.VerificationCase.VerifierUserId);
    }

    [Fact]
    public async Task Submit_NeverAssignsTheCaseToTheStudent()
    {
        await _profiles.AddAsync(new VerifierProfile(1, "http-basics"));

        var result = await Submit(Failing);

        Assert.Equal(CaseStatus.Pending, result.Value!.VerificationCase!.Status);
    }

    // ---------- Rejections ----------

    [Fact]
    public async Task Submit_ForAnUnknownBlueprint_FailsWithBlueprintNotFound()
    {
        AssertFailure(await Submit(blueprintId: 99), AssessmentPeerReviewError.BlueprintNotFound);
    }

    [Fact]
    public async Task Submit_ForAnotherStudentsBlueprint_FailsWithNotBlueprintOwner()
    {
        AssertFailure(await Submit(studentId: 2), AssessmentPeerReviewError.NotBlueprintOwner);
        Assert.Empty(_attempts.Attempts);
    }

    [Fact]
    public async Task Submit_WithTheWrongNumberOfAnswers_FailsWithInvalidAnswers()
    {
        AssertFailure(await Submit([1, 2, 3]), AssessmentPeerReviewError.InvalidAnswers);
    }

    [Fact]
    public async Task Submit_WithAnAnswerOutOfRange_FailsWithInvalidAnswers()
    {
        AssertFailure(await Submit([1, 2, 3, 0, 4]), AssessmentPeerReviewError.InvalidAnswers);
        AssertFailure(await Submit([1, 2, 3, 0, -1]), AssessmentPeerReviewError.InvalidAnswers);
    }

    [Fact]
    public async Task Submit_WhenTheNodeIsNotAvailable_FailsWithNodeNotAvailable()
    {
        _learningPath.AddBlueprint(nodeIsAvailable: false);

        AssertFailure(await Submit(), AssessmentPeerReviewError.NodeNotAvailable);
    }

    [Fact]
    public async Task Submit_WhenThereIsAnOpenCaseForTheNode_FailsWithOpenCaseAlreadyExists()
    {
        await _cases.AddAsync(new VerificationCase(5, 1, 10, "http-basics"));

        AssertFailure(await Submit(), AssessmentPeerReviewError.OpenCaseAlreadyExists);
        Assert.Empty(_attempts.Attempts);
    }

    [Fact]
    public async Task Submit_WhenTheCaseOfTheNodeWasResolved_IsAllowed()
    {
        var resolved = new VerificationCase(5, 1, 10, "http-basics").AssignVerifier(2)
            .Resolve(ReviewDecision.Rejected, "Needs work.");
        await _cases.AddAsync(resolved);

        Assert.True((await Submit()).IsSuccess);
    }

    [Fact]
    public async Task Submit_ForAnOutdatedBlueprint_FailsWithBlueprintOutdated()
    {
        _learningPath.AddBlueprint(isLatest: false);

        AssertFailure(await Submit(), AssessmentPeerReviewError.BlueprintOutdated);
    }

    [Fact]
    public async Task Submit_TwiceForTheSameBlueprint_FailsWithAttemptAlreadySubmitted()
    {
        await Submit();

        AssertFailure(await Submit(), AssessmentPeerReviewError.AttemptAlreadySubmitted);
        Assert.Single(_attempts.Attempts);
    }

    // ---------- Failures ----------

    [Fact]
    public async Task Submit_WhenTheNodeCannotBeCompleted_FailsAndPublishesNothing()
    {
        _learningPath.NextOutcome = NodeCompletionOutcome.NodeLocked;

        AssertFailure(await Submit(), AssessmentPeerReviewError.NodeNotAvailable);
        Assert.Empty(_events.Published);
    }

    [Fact]
    public async Task Submit_WhenCompletingTheNodeFails_FailsWithDatabaseError()
    {
        _learningPath.NextOutcome = NodeCompletionOutcome.Failed;

        AssertFailure(await Submit(), AssessmentPeerReviewError.DatabaseError);
        Assert.Empty(_events.Published);
    }

    [Fact]
    public async Task Submit_WhenPersistenceFails_FailsWithDatabaseError()
    {
        _unitOfWork.ExceptionToThrow = new DbUpdateException("failure");

        AssertFailure(await Submit(), AssessmentPeerReviewError.DatabaseError);
        Assert.Empty(_events.Published);
    }

    [Fact]
    public async Task Submit_WhenTheRequestIsCancelled_FailsWithOperationCancelled()
    {
        _unitOfWork.ExceptionToThrow = new OperationCanceledException();

        AssertFailure(await Submit(), AssessmentPeerReviewError.OperationCancelled);
    }

    [Fact]
    public async Task Submit_WhenSomethingUnexpectedFails_FailsWithInternalServerError()
    {
        _unitOfWork.ExceptionToThrow = new InvalidOperationException("boom");

        AssertFailure(await Submit(), AssessmentPeerReviewError.InternalServerError);
    }
}