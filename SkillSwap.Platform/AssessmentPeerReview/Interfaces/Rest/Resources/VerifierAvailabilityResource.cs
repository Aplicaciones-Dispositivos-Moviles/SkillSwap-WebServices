namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

/// <summary>
///     Resource for switching the availability of a verifier
/// </summary>
/// <param name="Available">True to accept new cases, false to stop receiving them. It is required.</param>
public record VerifierAvailabilityResource(bool? Available);