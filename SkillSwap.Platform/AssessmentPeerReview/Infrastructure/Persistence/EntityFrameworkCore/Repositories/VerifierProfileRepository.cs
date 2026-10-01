using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace SkillSwap.Platform.AssessmentPeerReview.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Verifier profile repository implementation over the "verifier_profiles" table
/// </summary>
/// <param name="context">The EF Core database context</param>
public class VerifierProfileRepository(AppDbContext context)
    : BaseRepository<VerifierProfile>(context), IVerifierProfileRepository
{
    /// <inheritdoc />
    public async Task<VerifierProfile?> FindByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await Context.Set<VerifierProfile>()
            .FirstOrDefaultAsync(p => p.VerifierUserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VerifierProfile>> FindEnabledBySkillTagAsync(string skillTag,
        CancellationToken cancellationToken)
    {
        // The skills live in a jsonb column that EF cannot search, so the skill is filtered in memory
        // over the (few) profiles that were not revoked.
        var enabled = await Context.Set<VerifierProfile>()
            .Where(p => p.Verified)
            .ToListAsync(cancellationToken);
        return enabled.Where(p => p.HasSkill(skillTag)).ToList();
    }
}