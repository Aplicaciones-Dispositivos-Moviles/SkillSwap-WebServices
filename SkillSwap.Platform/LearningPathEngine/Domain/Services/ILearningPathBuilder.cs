using SkillSwap.Platform.LearningPathEngine.Domain.Model.Entities;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.LearningPathEngine.Domain.Services;

/// <summary>
///     Contract for turning a skill gap into the ordered nodes of a path, respecting the prerequisites.
/// </summary>
public interface ILearningPathBuilder
{
    IReadOnlyList<PathNode> BuildPath(SkillGap gap);
}