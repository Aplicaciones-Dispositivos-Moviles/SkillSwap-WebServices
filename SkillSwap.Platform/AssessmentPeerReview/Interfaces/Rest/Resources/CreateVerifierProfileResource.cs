namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

/// <summary>
///     Resource for becoming a verifier of a skill
/// </summary>
/// <param name="SkillTag">The skill code, which the student must have completed</param>
public record CreateVerifierProfileResource(string? SkillTag);