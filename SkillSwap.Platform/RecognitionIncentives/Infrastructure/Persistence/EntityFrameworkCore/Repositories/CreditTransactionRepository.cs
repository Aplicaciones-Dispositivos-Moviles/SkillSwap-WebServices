using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.RecognitionIncentives.Domain.Repositories;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace SkillSwap.Platform.RecognitionIncentives.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Credit transaction repository implementation over the "credit_transactions" table
/// </summary>
/// <param name="context">The EF Core database context</param>
public class CreditTransactionRepository(AppDbContext context)
    : BaseRepository<CreditTransaction>(context), ICreditTransactionRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CreditTransaction>> FindByWalletIdAsync(int walletId,
        CancellationToken cancellationToken)
    {
        return await Context.Set<CreditTransaction>()
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsEarnedForCaseAsync(int walletId, int caseId, CancellationToken cancellationToken)
    {
        return await Context.Set<CreditTransaction>()
            .AnyAsync(t => t.WalletId == walletId
                           && t.Type == TransactionType.Earned
                           && t.RelatedCaseId == caseId, cancellationToken);
    }
}