using Microsoft.Extensions.Logging;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Events;
using SkillSwap.Platform.RecognitionIncentives.Application.CommandServices;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Commands;
using SkillSwap.Platform.Shared.Domain.Events;

namespace SkillSwap.Platform.RecognitionIncentives.Application.EventHandlers;

/// <summary>
///     Reacts to a verifier resolving a case, approved or rejected, by crediting their wallet. It is the only
///     place where credits are earned, and it depends only on that event: no user can give credits to another.
/// </summary>
/// <param name="walletCommandService">Wallet command service</param>
/// <param name="logger">Logger</param>
public class CreditVerifierEventHandler(
    IWalletCommandService walletCommandService,
    ILogger<CreditVerifierEventHandler> logger)
    : IDomainEventHandler<VerificationCaseResolved>
{
    public async Task HandleAsync(VerificationCaseResolved domainEvent, CancellationToken cancellationToken)
    {
        var result = await walletCommandService.Handle(
            new CreditVerifierCommand(domainEvent.VerifierUserId, domainEvent.CaseId), cancellationToken);
        if (result.IsFailure)
            logger.LogWarning("The case {CaseId} was not credited to verifier {VerifierUserId}: {Error}",
                domainEvent.CaseId, domainEvent.VerifierUserId, result.Error);
    }
}