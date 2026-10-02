namespace SkillSwap.Platform.Reputation.Domain.Model.Commands;

/// <summary>
///     Record case resolution command
/// </summary>
/// <param name="VerifierUserId">The verifier who resolved the case</param>
/// <param name="StudentId">The student whose case it was</param>
/// <param name="Approved">Whether the verifier approved it, which certifies the student's skill</param>
public record RecordCaseResolutionCommand(int VerifierUserId, int StudentId, bool Approved);