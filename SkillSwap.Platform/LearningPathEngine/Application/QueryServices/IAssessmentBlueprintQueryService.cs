using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Queries;

namespace SkillSwap.Platform.LearningPathEngine.Application.QueryServices;

/// <summary>
///     Assessment blueprint query service interface
/// </summary>
public interface IAssessmentBlueprintQueryService
{
    Task<AssessmentBlueprint?> Handle(GetAssessmentBlueprintByPathNodeIdQuery query,
        CancellationToken cancellationToken);
}