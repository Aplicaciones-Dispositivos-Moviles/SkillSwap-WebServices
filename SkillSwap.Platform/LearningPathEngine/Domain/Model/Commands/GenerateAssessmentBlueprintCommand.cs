namespace SkillSwap.Platform.LearningPathEngine.Domain.Model.Commands;

/// <summary>
///     Generate assessment blueprint command
/// </summary>
/// <param name="PathNodeId">The node whose assessment is requested</param>
/// <param name="StudentId">The authenticated student, who must own the path</param>
public record GenerateAssessmentBlueprintCommand(int PathNodeId, int StudentId);