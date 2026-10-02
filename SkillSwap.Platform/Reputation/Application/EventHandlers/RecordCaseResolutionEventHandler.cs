using Microsoft.Extensions.Logging;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Events;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.Reputation.Application.CommandServices;
using SkillSwap.Platform.Reputation.Domain.Model.Commands;
using SkillSwap.Platform.Shared.Domain.Events;

namespace SkillSwap.Platform.Reputation.Application.EventHandlers;

/// <summary>
///     Reacts to a verifier resolving a case: counts it in their reliability and, when it was approved, in the
///     employability of the student.
/// </summary>
/// <param name="reputationCommandService">Reputation command service</param>
/// <param name="logger">Logger</param>
public class RecordCaseResolutionEventHandler(
    IReputationCommandService reputationCommandService,
    ILogger<RecordCaseResolutionEventHandler> logger)
    : IDomainEventHandler<VerificationCaseResolved>
{
    public async Task HandleAsync(VerificationCaseResolved domainEvent, CancellationToken cancellationToken)
    {
        var command = new RecordCaseResolutionCommand(domainEvent.VerifierUserId, domainEvent.StudentId,
            domainEvent.Decision == ReviewDecision.Approved);

        var result = await reputationCommandService.Handle(command, cancellationToken);
        if (result.IsFailure)
            logger.LogWarning("The resolution of the case {CaseId} was not recorded in the reputation: {Error}",
                domainEvent.CaseId, result.Error);
    }
}