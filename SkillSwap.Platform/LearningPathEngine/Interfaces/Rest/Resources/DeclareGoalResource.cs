namespace SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;

/// <summary>
///     Resource for declaring a career goal in free text
/// </summary>
/// <param name="Goal">The goal in the student's own words (1 to 500 characters)</param>
public record DeclareGoalResource(string? Goal);