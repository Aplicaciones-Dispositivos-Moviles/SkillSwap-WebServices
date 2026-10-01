using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Queries;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.QueryServices;

public interface IVerifierProfileQueryService
{
    Task<VerifierProfile?> Handle(GetVerifierProfileByUserIdQuery query, CancellationToken cancellationToken);
}