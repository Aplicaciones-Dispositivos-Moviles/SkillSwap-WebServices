using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace SkillSwap.Platform.AssessmentPeerReview.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Assessment attempt repository implementation over the "assessment_attempts" table
/// </summary>
/// <param name="context">The EF Core database context</param>
public class AssessmentAttemptRepository(AppDbContext context)
    : BaseRepository<AssessmentAttempt>(context), IAssessmentAttemptRepository
{
    /// <inheritdoc />
    public async Task<AssessmentAttempt?> FindByBlueprintIdAsync(int blueprintId,
        CancellationToken cancellationToken)
    {
        return await Context.Set<AssessmentAttempt>()
            .FirstOrDefaultAsync(a => a.BlueprintId == blueprintId, cancellationToken);
    }
}