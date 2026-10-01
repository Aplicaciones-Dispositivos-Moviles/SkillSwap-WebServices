using SkillSwap.Platform.AssessmentPeerReview.Application.QueryServices;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Queries;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.Internal.QueryServices;

/// <summary>
///     Verifier profile query service
/// </summary>
/// <param name="profileRepository">Verifier profile repository</param>
public class VerifierProfileQueryService(IVerifierProfileRepository profileRepository)
    : IVerifierProfileQueryService
{
    /// <inheritdoc />
    public async Task<VerifierProfile?> Handle(GetVerifierProfileByUserIdQuery query,
        CancellationToken cancellationToken)
    {
        return await profileRepository.FindByUserIdAsync(query.UserId, cancellationToken);
    }
}