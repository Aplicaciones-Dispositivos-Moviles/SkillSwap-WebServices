using SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;
using SkillSwap.Platform.Shared.Domain.Repositories;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.ACL;

/// <summary>
///     Facade implementation over the verifier profile repository
/// </summary>
/// <param name="profileRepository">Verifier profile repository</param>
/// <param name="unitOfWork">Unit of work</param>
public class VerifierProfileContextFacade(
    IVerifierProfileRepository profileRepository,
    IUnitOfWork unitOfWork) : IVerifierProfileContextFacade
{
    /// <inheritdoc />
    public async Task<bool> UpdateRatingAsync(int verifierUserId, double rating,
        CancellationToken cancellationToken)
    {
        var profile = await profileRepository.FindByUserIdAsync(verifierUserId, cancellationToken);
        if (profile is null) return false;

        // A revoked profile keeps receiving its rating: the history stays accurate if it is restored.
        profile.UpdateRating(rating);
        profileRepository.Update(profile);
        await unitOfWork.CompleteAsync(cancellationToken);
        return true;
    }
}