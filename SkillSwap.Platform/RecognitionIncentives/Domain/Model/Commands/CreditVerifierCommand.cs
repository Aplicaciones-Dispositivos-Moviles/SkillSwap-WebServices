namespace SkillSwap.Platform.RecognitionIncentives.Domain.Model.Commands;

/// <summary>
///     Credit verifier command
/// </summary>
/// <param name="VerifierUserId">The verifier who resolved the case</param>
/// <param name="CaseId">The resolved verification case, which can credit only once</param>
public record CreditVerifierCommand(int VerifierUserId, int CaseId);