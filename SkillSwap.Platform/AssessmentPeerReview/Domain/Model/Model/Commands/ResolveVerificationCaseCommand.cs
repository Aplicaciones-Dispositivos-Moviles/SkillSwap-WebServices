// ResolveVerificationCaseCommand.cs
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;

/// <summary>
///     Resolve verification case command
/// </summary>
/// <param name="CaseId">The case being resolved</param>
/// <param name="VerifierUserId">The authenticated verifier, who must be the assigned one</param>
/// <param name="Decision">Approved or rejected</param>
/// <param name="RubricNotes">The verifier's observations following the rubric</param>
public record ResolveVerificationCaseCommand(
    int CaseId,
    int VerifierUserId,
    ReviewDecision Decision,
    string RubricNotes);