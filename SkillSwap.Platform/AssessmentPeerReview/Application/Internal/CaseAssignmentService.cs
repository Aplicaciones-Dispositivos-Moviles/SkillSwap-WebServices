using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Services;
using SkillSwap.Platform.Shared.Domain.Repositories;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.Internal;

/// <summary>
///     Case assignment service
/// </summary>
/// <param name="profileRepository">Verifier profile repository</param>
/// <param name="caseRepository">Verification case repository</param>
/// <param name="verifierMatcher">Chooses the verifier</param>
/// <param name="unitOfWork">Unit of work</param>
public class CaseAssignmentService(
    IVerifierProfileRepository profileRepository,
    IVerificationCaseRepository caseRepository,
    IVerifierMatcher verifierMatcher,
    IUnitOfWork unitOfWork) : ICaseAssignmentService
{
    /// <inheritdoc />
    public async Task<bool> TryAssignAsync(VerificationCase verificationCase, CancellationToken cancellationToken)
    {
        var profiles = await profileRepository.FindEnabledBySkillTagAsync(verificationCase.SkillTag,
            cancellationToken);
        if (profiles.Count == 0) return false;

        var openCases = await caseRepository.CountOpenByVerifierUserIdsAsync(
            profiles.Select(p => p.VerifierUserId).ToList(), cancellationToken);
        var candidates = profiles.Select(p => new VerifierCandidate(p, openCases.GetValueOrDefault(p.VerifierUserId)));

        var chosen = verifierMatcher.FindVerifier(verificationCase.SkillTag, verificationCase.StudentId, candidates);
        if (chosen is null) return false;

        verificationCase.AssignVerifier(chosen.VerifierUserId);
        return true;
    }

    /// <inheritdoc />
    public async Task<int> AssignPendingAsync(IEnumerable<string> skillTags, CancellationToken cancellationToken)
    {
        var assigned = 0;
        foreach (var skillTag in skillTags.Distinct(StringComparer.Ordinal))
        {
            var pending = await caseRepository.FindPendingBySkillTagAsync(skillTag, cancellationToken);
            foreach (var verificationCase in pending)
            {
                if (!await TryAssignAsync(verificationCase, cancellationToken)) continue;

                caseRepository.Update(verificationCase);
                await unitOfWork.CompleteAsync(cancellationToken);
                assigned++;
            }
        }

        return assigned;
    }
}