using SkillSwap.Platform.LearningPathEngine.Application.QueryServices;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Queries;
using SkillSwap.Platform.LearningPathEngine.Domain.Repositories;

namespace SkillSwap.Platform.LearningPathEngine.Application.Internal.QueryServices;

/// <summary>
///     Assessment blueprint query service
/// </summary>
/// <param name="blueprintRepository">Assessment blueprint repository</param>
public class AssessmentBlueprintQueryService(IAssessmentBlueprintRepository blueprintRepository)
    : IAssessmentBlueprintQueryService
{
    /// <inheritdoc />
    public async Task<AssessmentBlueprint?> Handle(GetAssessmentBlueprintByPathNodeIdQuery query,
        CancellationToken cancellationToken)
    {
        return await blueprintRepository.FindLatestByPathNodeIdAsync(query.PathNodeId, cancellationToken);
    }
}