namespace SkillSwap.Platform.Reputation.Domain.Model.Commands;

/// <summary>
///     Record automatic approval command
/// </summary>
/// <param name="StudentId">The student who passed the assessment without a verifier</param>
public record RecordAutomaticApprovalCommand(int StudentId);