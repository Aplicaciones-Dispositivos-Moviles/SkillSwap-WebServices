// IAssessmentAttemptRepository.cs
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.Shared.Domain.Repositories;

namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;

public interface IAssessmentAttemptRepository : IBaseRepository<AssessmentAttempt>
{
    /// <summary>
    ///     The attempt already submitted for the blueprint, if any.
    /// </summary>
    Task<AssessmentAttempt?> FindByBlueprintIdAsync(int blueprintId, CancellationToken cancellationToken);
}