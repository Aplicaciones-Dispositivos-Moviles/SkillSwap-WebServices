using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Commands;
using SkillSwap.Platform.Shared.Application.Model;

namespace SkillSwap.Platform.LearningPathEngine.Application.CommandServices;

/// <summary>
///     Assessment blueprint command service interface
/// </summary>
public interface IAssessmentBlueprintCommandService
{
    /// <summary>
    ///     Handle generate assessment blueprint command for an available node.
    /// </summary>
    Task<Result<AssessmentBlueprint>> Handle(GenerateAssessmentBlueprintCommand command,
        CancellationToken cancellationToken);
}