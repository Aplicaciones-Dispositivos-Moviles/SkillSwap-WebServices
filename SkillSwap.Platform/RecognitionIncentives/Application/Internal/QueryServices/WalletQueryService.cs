using SkillSwap.Platform.RecognitionIncentives.Application.QueryServices;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Queries;
using SkillSwap.Platform.RecognitionIncentives.Domain.Repositories;

namespace SkillSwap.Platform.RecognitionIncentives.Application.Internal.QueryServices;

/// <summary>
///     Wallet query service
/// </summary>
/// <param name="walletRepository">Wallet repository</param>
/// <param name="transactionRepository">Credit transaction repository</param>
public class WalletQueryService(
    IWalletRepository walletRepository,
    ICreditTransactionRepository transactionRepository)
    : IWalletQueryService
{
    /// <inheritdoc />
    public async Task<Wallet?> Handle(GetWalletByOwnerIdQuery query, CancellationToken cancellationToken)
    {
        return await walletRepository.FindByOwnerIdAsync(query.OwnerId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CreditTransaction>?> Handle(GetWalletTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        var wallet = await walletRepository.FindByOwnerIdAsync(query.OwnerId, cancellationToken);
        if (wallet is null) return null;

        return await transactionRepository.FindByWalletIdAsync(wallet.Id, cancellationToken);
    }
}