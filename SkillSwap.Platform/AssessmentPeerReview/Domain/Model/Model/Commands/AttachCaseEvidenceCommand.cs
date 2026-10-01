// AttachCaseEvidenceCommand.cs
namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;

/// <summary>
///     Attach case evidence command
/// </summary>
/// <param name="CaseId">The case that receives the evidence</param>
/// <param name="StudentId">The authenticated student, who must own the case</param>
/// <param name="EvidenceUrl">Link to the student's repository or portfolio</param>
public record AttachCaseEvidenceCommand(int CaseId, int StudentId, string EvidenceUrl);