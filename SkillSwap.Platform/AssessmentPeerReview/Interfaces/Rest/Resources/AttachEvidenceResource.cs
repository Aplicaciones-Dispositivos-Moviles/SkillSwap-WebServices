namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

/// <summary>
///     Resource for attaching evidence to a verification case
/// </summary>
/// <param name="EvidenceUrl">Link to the student's repository or portfolio (http or https, up to 500 characters)</param>
public record AttachEvidenceResource(string? EvidenceUrl);