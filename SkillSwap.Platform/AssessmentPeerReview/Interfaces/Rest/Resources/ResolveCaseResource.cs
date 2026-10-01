namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

/// <summary>
///     Resource for resolving a verification case
/// </summary>
/// <param name="Decision">Approved or Rejected</param>
/// <param name="RubricNotes">The verifier's observations following the rubric (1 to 2000 characters)</param>
public record ResolveCaseResource(string? Decision, string? RubricNotes);