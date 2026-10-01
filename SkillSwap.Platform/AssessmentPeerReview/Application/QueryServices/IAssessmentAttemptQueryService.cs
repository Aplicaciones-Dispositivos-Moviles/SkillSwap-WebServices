using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Queries;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.QueryServices;

public interface IAssessmentAttemptQueryService
{
    Task<AssessmentAttempt?> Handle(GetAssessmentAttemptByIdQuery query, CancellationToken cancellationToken);
}