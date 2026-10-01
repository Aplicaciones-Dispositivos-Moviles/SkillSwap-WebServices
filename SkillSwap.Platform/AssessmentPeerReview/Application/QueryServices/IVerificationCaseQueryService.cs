using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Queries;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.QueryServices;

public interface IVerificationCaseQueryService
{
    Task<VerificationCase?> Handle(GetVerificationCaseByIdQuery query, CancellationToken cancellationToken);

    Task<VerificationCaseDetail?> Handle(GetVerificationCaseDetailQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyList<VerificationCase>> Handle(GetVerificationCasesByVerifierQuery query,
        CancellationToken cancellationToken);
}