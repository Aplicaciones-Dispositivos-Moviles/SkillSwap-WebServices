using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.Shared.Domain.Repositories;

namespace SkillSwap.Platform.LearningPathEngine.Domain.Repositories;

/// <summary>
///     Learning path repository interface
/// </summary>
public interface ILearningPathRepository : IBaseRepository<LearningPath>
{
    /// <summary>
    ///     The most recent path of a student. Since a student has at most one active path, that is
    ///     the active one whenever there is one.
    /// </summary>
    Task<LearningPath?> FindLatestByStudentIdAsync(int studentId, CancellationToken cancellationToken);

    /// <summary>
    ///     The path that contains the given node.
    /// </summary>
    Task<LearningPath?> FindByNodeIdAsync(int nodeId, CancellationToken cancellationToken);

    /// <summary>
    ///     The skills the student completed in any of their paths.
    /// </summary>
    Task<IReadOnlyCollection<string>> FindCompletedSkillTagsByStudentIdAsync(int studentId,
        CancellationToken cancellationToken);
}