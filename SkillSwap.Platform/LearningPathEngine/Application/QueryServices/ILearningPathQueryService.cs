using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Queries;

namespace SkillSwap.Platform.LearningPathEngine.Application.QueryServices;

/// <summary>
///     Learning path query service interface
/// </summary>
public interface ILearningPathQueryService
{
    Task<LearningPath?> Handle(GetLearningPathByStudentIdQuery query, CancellationToken cancellationToken);
}