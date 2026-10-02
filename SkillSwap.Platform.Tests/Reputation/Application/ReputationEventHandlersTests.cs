using Microsoft.Extensions.Logging.Abstractions;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Events;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.Reputation.Application.CommandServices;
using SkillSwap.Platform.Reputation.Application.EventHandlers;
using SkillSwap.Platform.Reputation.Domain.Model;
using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Model.Commands;
using SkillSwap.Platform.Shared.Application.Model;

namespace SkillSwap.Platform.Tests.Reputation.Application;

public class ReputationEventHandlersTests
{
    private readonly RecordingReputationCommandService _service = new();

    [Theory]
    [InlineData(ReviewDecision.Approved, true)]
    [InlineData(ReviewDecision.Rejected, false)]
    public async Task CaseResolved_BecomesACommandWithTheDecision(ReviewDecision decision, bool approved)
    {
        var handler = new RecordCaseResolutionEventHandler(_service,
            NullLogger<RecordCaseResolutionEventHandler>.Instance);

        await handler.HandleAsync(new VerificationCaseResolved(10, 1, 2, 5, "http-basics", decision),
            CancellationToken.None);

        Assert.Equal(new RecordCaseResolutionCommand(2, 1, approved), Assert.Single(_service.Resolutions));
        Assert.Empty(_service.Approvals);
    }

    [Fact]
    public async Task CaseResolved_WhenTheServiceFails_DoesNotThrow()
    {
        _service.Fail = true;
        var handler = new RecordCaseResolutionEventHandler(_service,
            NullLogger<RecordCaseResolutionEventHandler>.Instance);

        await handler.HandleAsync(new VerificationCaseResolved(10, 1, 2, 5, "http-basics", ReviewDecision.Approved),
            CancellationToken.None);

        Assert.Single(_service.Resolutions);
    }

    [Fact]
    public async Task AttemptPassed_BecomesAnAutomaticApprovalOfTheStudent()
    {
        var handler = new RecordAutomaticApprovalEventHandler(_service,
            NullLogger<RecordAutomaticApprovalEventHandler>.Instance);

        await handler.HandleAsync(new AssessmentAttemptPassed(7, 1, 5, "http-basics"), CancellationToken.None);

        Assert.Equal(new RecordAutomaticApprovalCommand(1), Assert.Single(_service.Approvals));
        Assert.Empty(_service.Resolutions);
    }

    [Fact]
    public async Task AttemptPassed_WhenTheServiceFails_DoesNotThrow()
    {
        _service.Fail = true;
        var handler = new RecordAutomaticApprovalEventHandler(_service,
            NullLogger<RecordAutomaticApprovalEventHandler>.Instance);

        await handler.HandleAsync(new AssessmentAttemptPassed(7, 1, 5, "http-basics"), CancellationToken.None);

        Assert.Single(_service.Approvals);
    }

    private sealed class RecordingReputationCommandService : IReputationCommandService
    {
        public List<RecordCaseResolutionCommand> Resolutions { get; } = [];
        public List<RecordAutomaticApprovalCommand> Approvals { get; } = [];
        public bool Fail { get; set; }

        public Task<Result<VerifierReliability>> Handle(RecordCaseResolutionCommand command,
            CancellationToken cancellationToken)
        {
            Resolutions.Add(command);
            return Task.FromResult(Fail
                ? Result<VerifierReliability>.Failure(ReputationError.DatabaseError, "failure")
                : Result<VerifierReliability>.Success(new VerifierReliability(command.VerifierUserId)));
        }

        public Task<Result<StudentEmployabilityScore>> Handle(RecordAutomaticApprovalCommand command,
            CancellationToken cancellationToken)
        {
            Approvals.Add(command);
            return Task.FromResult(Fail
                ? Result<StudentEmployabilityScore>.Failure(ReputationError.DatabaseError, "failure")
                : Result<StudentEmployabilityScore>.Success(new StudentEmployabilityScore(command.StudentId)));
        }
    }
}