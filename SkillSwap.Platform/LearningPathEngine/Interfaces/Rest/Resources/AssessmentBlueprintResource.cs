namespace SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;

/// <summary>
///     A question of an assessment as the student sees it. The correct answer is deliberately absent:
///     it never leaves the server.
/// </summary>
/// <param name="Question">The question text</param>
/// <param name="Answers">The four answer options, in a fixed order</param>
public record QuestionResource(string Question, IReadOnlyList<string> Answers);

/// <summary>
///     Assessment blueprint resource for REST API
/// </summary>
/// <param name="Id">The unique identifier of the blueprint</param>
/// <param name="PathNodeId">The node the assessment belongs to</param>
/// <param name="SkillTag">The skill code being assessed</param>
/// <param name="SkillName">The display name of the skill</param>
/// <param name="Questions">The questions, without their correct answers</param>
/// <param name="GeneratedAt">When the assessment was generated (UTC)</param>
public record AssessmentBlueprintResource(
    int Id,
    int PathNodeId,
    string SkillTag,
    string SkillName,
    IReadOnlyList<QuestionResource> Questions,
    DateTime GeneratedAt);