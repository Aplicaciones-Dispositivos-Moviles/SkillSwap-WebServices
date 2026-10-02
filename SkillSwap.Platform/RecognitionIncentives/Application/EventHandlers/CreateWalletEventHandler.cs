using Microsoft.Extensions.Logging;
using SkillSwap.Platform.Iam.Domain.Model.Events;
using SkillSwap.Platform.RecognitionIncentives.Application.CommandServices;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Commands;
using SkillSwap.Platform.Shared.Domain.Events;

namespace SkillSwap.Platform.RecognitionIncentives.Application.EventHandlers;

/// <summary>
///     Reacts to a new account by creating its empty wallet.
/// </summary>
/// <param name="walletCommandService">Wallet command service</param>
/// <param name="logger">Logger</param>
public class CreateWalletEventHandler(
    IWalletCommandService walletCommandService,
    ILogger<CreateWalletEventHandler> logger)
    : IDomainEventHandler<UserRegistered>
{
    public async Task HandleAsync(UserRegistered domainEvent, CancellationToken cancellationToken)
    {
        var result = await walletCommandService.Handle(new CreateWalletCommand(domainEvent.UserId),
            cancellationToken);
        if (result.IsFailure)
            logger.LogWarning("The wallet of user {UserId} was not created: {Error}", domainEvent.UserId,
                result.Error);
    }
}