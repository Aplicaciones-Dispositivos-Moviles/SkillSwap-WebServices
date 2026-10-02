using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Queries;

namespace SkillSwap.Platform.RecognitionIncentives.Application.QueryServices;

public interface IWalletQueryService
{
    Task<Wallet?> Handle(GetWalletByOwnerIdQuery query, CancellationToken cancellationToken);

    /// <summary>
    ///     The movements of the user's wallet, newest first; null when the user has no wallet.
    /// </summary>
    Task<IReadOnlyList<CreditTransaction>?> Handle(GetWalletTransactionsQuery query,
        CancellationToken cancellationToken);
}