using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.Shared.Domain.Repositories;

namespace SkillSwap.Platform.LearningPathEngine.Domain.Repositories;

/// <summary>
///     Assessment blueprint repository interface
/// </summary>
public interface IAssessmentBlueprintRepository : IBaseRepository<AssessmentBlueprint>
{
    /// <summary>
    ///     The most recently generated blueprint of a node (older ones are kept as history).
    /// </summary>
    Task<AssessmentBlueprint?> FindLatestByPathNodeIdAsync(int pathNodeId, CancellationToken cancellationToken);
}