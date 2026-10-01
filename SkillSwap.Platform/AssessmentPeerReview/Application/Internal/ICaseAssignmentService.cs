using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.Internal;

/// <summary>
///     Assigns verification cases to the best qualified verifier.
/// </summary>
public interface ICaseAssignmentService
{
    /// <summary>
    ///     Assigns the case to the best verifier, without saving.
    /// </summary>
    /// <returns>True when a verifier was assigned; false when nobody qualifies and the case stays pending.</returns>
    Task<bool> TryAssignAsync(VerificationCase verificationCase, CancellationToken cancellationToken);

    /// <summary>
    ///     Assigns, oldest first, the pending cases of the skills to whoever qualifies now, saving after each
    ///     one so the next assignment sees the updated workload.
    /// </summary>
    /// <returns>How many cases were assigned.</returns>
    Task<int> AssignPendingAsync(IEnumerable<string> skillTags, CancellationToken cancellationToken);
}