namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

/// <summary>
///     Resource for submitting the answers of an assessment
/// </summary>
/// <param name="BlueprintId">The assessment being answered, as returned when it was generated</param>
/// <param name="SelectedAnswers">The index (0 to 3) of the chosen option for each question, in order</param>
public record SubmitAssessmentAttemptResource(int BlueprintId, IReadOnlyList<int>? SelectedAnswers);