// VerifierMatcher.cs
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;

namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Services;

/// <summary>
///     Assigns cases to the least loaded qualified verifier
/// </summary>
public class VerifierMatcher : IVerifierMatcher
{
    /// <inheritdoc />
    public VerifierProfile? FindVerifier(string skillTag, int studentId, IEnumerable<VerifierCandidate> candidates)
    {
        return candidates
            .Where(c => c.Profile.Available
                        && c.Profile.CanReview(skillTag)
                        && c.Profile.VerifierUserId != studentId)
            .OrderBy(c => c.OpenCaseCount)
            .ThenBy(c => c.Profile.VerifierUserId)
            .Select(c => c.Profile)
            .FirstOrDefault();
    }
}