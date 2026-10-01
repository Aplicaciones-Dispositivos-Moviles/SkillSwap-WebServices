using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;
using SkillSwap.Platform.LearningPathEngine.Domain.Repositories;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace SkillSwap.Platform.LearningPathEngine.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Learning path repository implementation over the "learning_paths" and "path_nodes" tables
/// </summary>
/// <param name="context">The EF Core database context</param>
public class LearningPathRepository(AppDbContext context)
    : BaseRepository<LearningPath>(context), ILearningPathRepository
{
    /// <inheritdoc />
    public async Task<LearningPath?> FindLatestByStudentIdAsync(int studentId, CancellationToken cancellationToken)
    {
        return await Context.Set<LearningPath>()
            .Include(p => p.Nodes)
            .Where(p => p.StudentId == studentId)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LearningPath?> FindByNodeIdAsync(int nodeId, CancellationToken cancellationToken)
    {
        return await Context.Set<LearningPath>()
            .Include(p => p.Nodes)
            .FirstOrDefaultAsync(p => p.Nodes.Any(n => n.Id == nodeId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>> FindCompletedSkillTagsByStudentIdAsync(int studentId,
        CancellationToken cancellationToken)
    {
        return await Context.Set<LearningPath>()
            .Where(p => p.StudentId == studentId)
            .SelectMany(p => p.Nodes)
            .Where(n => n.Status == NodeStatus.Completed)
            .Select(n => n.SkillTag)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}