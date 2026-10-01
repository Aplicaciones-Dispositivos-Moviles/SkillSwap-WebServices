// IVerifierMatcher.cs
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;

namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Services;

/// <summary>
///     A verifier together with the number of cases they have not resolved yet.
/// </summary>
public sealed record VerifierCandidate(VerifierProfile Profile, int OpenCaseCount);

/// <summary>
///     Chooses the verifier who takes a case.
/// </summary>
public interface IVerifierMatcher
{
    /// <summary>
    ///     The available verifier enabled for the skill with the fewest open cases, never the student
    ///     of the case; ties go to the lowest user id. Null when nobody qualifies.
    /// </summary>
    VerifierProfile? FindVerifier(string skillTag, int studentId, IEnumerable<VerifierCandidate> candidates);
}