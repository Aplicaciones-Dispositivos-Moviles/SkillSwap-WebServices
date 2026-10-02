namespace SkillSwap.Platform.AssessmentPeerReview.Application.ACL;

/// <summary>
///     Anti-corruption facade through which other bounded contexts use the verifier profiles of Assessment &amp;
///     Peer Review, without depending on its aggregates or repositories.
/// </summary>
public interface IVerifierProfileContextFacade
{
    /// <summary>
    ///     Stores the reliability that Reputation calculated as the rating of the verifier. The review count is
    ///     not touched: it belongs to Assessment &amp; Peer Review.
    /// </summary>
    /// <returns>True when the rating was updated; false when the user has no verifier profile.</returns>
    Task<bool> UpdateRatingAsync(int verifierUserId, double rating, CancellationToken cancellationToken);
}