namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

/// <summary>
///     Assessment attempt resource for REST API
/// </summary>
/// <param name="Id">The unique identifier of the attempt</param>
/// <param name="BlueprintId">The assessment that was answered</param>
/// <param name="StudentId">The student who answered it</param>
/// <param name="Score">Number of correct answers</param>
/// <param name="TotalQuestions">Number of questions of the assessment</param>
/// <param name="Passed">Whether the attempt approved the node automatically</param>
/// <param name="CompletedAt">When the attempt was graded (UTC)</param>
/// <param name="VerificationCaseId">The verification case opened by a failed attempt, when known</param>
/// <param name="VerificationCaseStatus">Pending or Assigned, when the case is known</param>
public record AssessmentAttemptResource(
    int Id,
    int BlueprintId,
    int StudentId,
    int Score,
    int TotalQuestions,
    bool Passed,
    DateTime CompletedAt,
    int? VerificationCaseId,
    string? VerificationCaseStatus);