using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Repositories;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace SkillSwap.Platform.Reputation.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Verifier reliability repository implementation over the "verifier_reliabilities" table
/// </summary>
/// <param name="context">The EF Core database context</param>
public class VerifierReliabilityRepository(AppDbContext context)
    : BaseRepository<VerifierReliability>(context), IVerifierReliabilityRepository
{
    /// <inheritdoc />
    public async Task<VerifierReliability?> FindByVerifierUserIdAsync(int verifierUserId,
        CancellationToken cancellationToken)
    {
        return await Context.Set<VerifierReliability>()
            .FirstOrDefaultAsync(r => r.VerifierUserId == verifierUserId, cancellationToken);
    }
}