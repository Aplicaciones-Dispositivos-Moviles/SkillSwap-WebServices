using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Commands;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.Shared.Application.Model;

namespace SkillSwap.Platform.RecognitionIncentives.Application.CommandServices;

public interface IWalletCommandService
{
    /// <summary>
    ///     Creates the empty wallet of a user. When the user already has one, it is returned unchanged.
    /// </summary>
    Task<Result<Wallet>> Handle(CreateWalletCommand command, CancellationToken cancellationToken);

    /// <summary>
    ///     Pays the verifier for a resolved case, creating the wallet if it does not exist yet. A case pays only
    ///     once: crediting it again succeeds without changing the balance.
    /// </summary>
    Task<Result<Wallet>> Handle(CreditVerifierCommand command, CancellationToken cancellationToken);

    /// <summary>
    ///     Redeems a benefit, taking its cost from the balance. Returns the recorded movement.
    /// </summary>
    Task<Result<CreditTransaction>> Handle(RedeemCommand command, CancellationToken cancellationToken);
}