using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Repositories;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace SkillSwap.Platform.LearningPathEngine.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Assessment blueprint repository implementation over the "assessment_blueprints" table
/// </summary>
/// <param name="context">The EF Core database context</param>
public class AssessmentBlueprintRepository(AppDbContext context)
    : BaseRepository<AssessmentBlueprint>(context), IAssessmentBlueprintRepository
{
    /// <inheritdoc />
    public async Task<AssessmentBlueprint?> FindLatestByPathNodeIdAsync(int pathNodeId,
        CancellationToken cancellationToken)
    {
        return await Context.Set<AssessmentBlueprint>()
            .Where(b => b.PathNodeId == pathNodeId)
            .OrderByDescending(b => b.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}