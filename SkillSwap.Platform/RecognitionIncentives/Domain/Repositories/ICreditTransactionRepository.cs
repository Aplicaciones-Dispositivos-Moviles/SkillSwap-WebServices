using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.Shared.Domain.Repositories;

namespace SkillSwap.Platform.RecognitionIncentives.Domain.Repositories;

public interface ICreditTransactionRepository : IBaseRepository<CreditTransaction>
{
    /// <summary>
    ///     The movements of a wallet, newest first.
    /// </summary>
    Task<IReadOnlyList<CreditTransaction>> FindByWalletIdAsync(int walletId, CancellationToken cancellationToken);

    /// <summary>
    ///     Whether the wallet already earned credits for the case.
    /// </summary>
    Task<bool> ExistsEarnedForCaseAsync(int walletId, int caseId, CancellationToken cancellationToken);
}