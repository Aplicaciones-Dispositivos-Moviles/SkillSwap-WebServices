using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.RecognitionIncentives.Domain.Repositories;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace SkillSwap.Platform.RecognitionIncentives.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Wallet repository implementation over the "wallets" table
/// </summary>
/// <param name="context">The EF Core database context</param>
public class WalletRepository(AppDbContext context) : BaseRepository<Wallet>(context), IWalletRepository
{
    /// <inheritdoc />
    public async Task<Wallet?> FindByOwnerIdAsync(int ownerId, CancellationToken cancellationToken)
    {
        return await Context.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.WalletOwnerId == ownerId, cancellationToken);
    }
}