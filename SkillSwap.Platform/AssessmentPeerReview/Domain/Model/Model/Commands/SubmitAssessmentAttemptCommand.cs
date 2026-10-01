// SubmitAssessmentAttemptCommand.cs
namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;

/// <summary>
///     Submit assessment attempt command
/// </summary>
/// <param name="StudentId">The authenticated student (taken from the token, never from the body)</param>
/// <param name="BlueprintId">The blueprint answered</param>
/// <param name="SelectedAnswers">The index of the chosen answer for each question, in order</param>
public record SubmitAssessmentAttemptCommand(int StudentId, int BlueprintId, IReadOnlyList<int> SelectedAnswers);