using SkillSwap.Platform.AssessmentPeerReview.Application.QueryServices;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Queries;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.Internal.QueryServices;

/// <summary>
///     Assessment attempt query service
/// </summary>
/// <param name="attemptRepository">Assessment attempt repository</param>
public class AssessmentAttemptQueryService(IAssessmentAttemptRepository attemptRepository)
    : IAssessmentAttemptQueryService
{
    /// <inheritdoc />
    public async Task<AssessmentAttempt?> Handle(GetAssessmentAttemptByIdQuery query,
        CancellationToken cancellationToken)
    {
        return await attemptRepository.FindByIdAsync(query.AttemptId, cancellationToken);
    }
}