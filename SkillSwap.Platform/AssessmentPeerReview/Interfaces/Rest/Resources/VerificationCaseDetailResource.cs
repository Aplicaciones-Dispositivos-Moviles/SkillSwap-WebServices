namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

/// <summary>
///     A question the student answered incorrectly. The correct answer is deliberately absent.
/// </summary>
/// <param name="Position">Position of the question in the assessment, starting at 1</param>
/// <param name="Question">The question text</param>
/// <param name="Answers">The answer options</param>
/// <param name="SelectedAnswer">The index of the option the student chose</param>
public record FailedQuestionResource(int Position, string Question, IReadOnlyList<string> Answers, int SelectedAnswer);

/// <summary>
///     A verification case with the attempt that opened it and the questions that were failed
/// </summary>
/// <param name="Case">The case</param>
/// <param name="Attempt">The failed attempt</param>
/// <param name="FailedQuestions">The questions answered incorrectly, with the option the student chose</param>
public record VerificationCaseDetailResource(
    VerificationCaseResource Case,
    AssessmentAttemptResource Attempt,
    IReadOnlyList<FailedQuestionResource> FailedQuestions);