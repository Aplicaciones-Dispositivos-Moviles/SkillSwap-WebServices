using Microsoft.Extensions.Logging;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Events;
using SkillSwap.Platform.Reputation.Application.CommandServices;
using SkillSwap.Platform.Reputation.Domain.Model.Commands;
using SkillSwap.Platform.Shared.Domain.Events;

namespace SkillSwap.Platform.Reputation.Application.EventHandlers;

/// <summary>
///     Reacts to a student passing an assessment without a verifier: counts the skill in their employability.
/// </summary>
/// <param name="reputationCommandService">Reputation command service</param>
/// <param name="logger">Logger</param>
public class RecordAutomaticApprovalEventHandler(
    IReputationCommandService reputationCommandService,
    ILogger<RecordAutomaticApprovalEventHandler> logger)
    : IDomainEventHandler<AssessmentAttemptPassed>
{
    public async Task HandleAsync(AssessmentAttemptPassed domainEvent, CancellationToken cancellationToken)
    {
        var result = await reputationCommandService.Handle(new RecordAutomaticApprovalCommand(domainEvent.StudentId),
            cancellationToken);
        if (result.IsFailure)
            logger.LogWarning("The approval of the attempt {AttemptId} was not recorded in the reputation: {Error}",
                domainEvent.AttemptId, result.Error);
    }
}