namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

/// <summary>
///     Verification case resource for REST API
/// </summary>
/// <param name="Id">The unique identifier of the case</param>
/// <param name="AttemptId">The failed attempt that opened the case</param>
/// <param name="StudentId">The student who demonstrates the skill</param>
/// <param name="VerifierUserId">The assigned verifier; null while the case is pending</param>
/// <param name="PathNodeId">The node being demonstrated</param>
/// <param name="SkillTag">The skill code being demonstrated</param>
/// <param name="Status">Pending, Assigned or Resolved</param>
/// <param name="Decision">Approved or Rejected, once resolved</param>
/// <param name="RubricNotes">The verifier's observations, once resolved</param>
/// <param name="EvidenceUrl">Link to the student's repository or portfolio, if attached</param>
/// <param name="OpenedAt">When the case was opened (UTC)</param>
/// <param name="AssignedAt">When a verifier was assigned (UTC)</param>
/// <param name="ResolvedAt">When the case was resolved (UTC)</param>
public record VerificationCaseResource(
    int Id,
    int AttemptId,
    int StudentId,
    int? VerifierUserId,
    int PathNodeId,
    string SkillTag,
    string Status,
    string? Decision,
    string? RubricNotes,
    string? EvidenceUrl,
    DateTime OpenedAt,
    DateTime? AssignedAt,
    DateTime? ResolvedAt);