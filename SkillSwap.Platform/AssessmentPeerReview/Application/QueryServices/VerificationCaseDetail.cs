using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.QueryServices;

/// <summary>
///     A question the student answered incorrectly. It never carries the correct answer.
/// </summary>
/// <param name="Position">The position of the question in the assessment, starting at 1</param>
/// <param name="Text">The question</param>
/// <param name="Answers">The options</param>
/// <param name="SelectedAnswer">The index of the option the student chose</param>
public sealed record FailedQuestion(int Position, string Text, IReadOnlyList<string> Answers, int SelectedAnswer);

/// <summary>
///     A verification case with the attempt that opened it and the questions that were failed.
/// </summary>
public sealed record VerificationCaseDetail(
    VerificationCase Case,
    AssessmentAttempt Attempt,
    IReadOnlyList<FailedQuestion> FailedQuestions);