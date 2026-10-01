namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

/// <summary>
///     Verifier profile resource for REST API
/// </summary>
/// <param name="Id">The unique identifier of the profile</param>
/// <param name="VerifierUserId">The user who is the verifier</param>
/// <param name="SkillTags">The skills the verifier is enabled to review</param>
/// <param name="Available">Whether the verifier accepts new cases</param>
/// <param name="Verified">Whether the profile is still enabled</param>
/// <param name="Rating">Reliability calculated by Reputation; zero until then</param>
/// <param name="ReviewCount">Number of cases the verifier resolved</param>
/// <param name="CreatedAt">When the profile was created (UTC)</param>
public record VerifierProfileResource(
    int Id,
    int VerifierUserId,
    IReadOnlyList<string> SkillTags,
    bool Available,
    bool Verified,
    double Rating,
    int ReviewCount,
    DateTime CreatedAt);