namespace SkillSwap.Platform.LearningPathEngine.Domain.Model.Commands;

/// <summary>
///     Declare goal command
/// </summary>
/// <param name="StudentId">The authenticated student (taken from the token)</param>
/// <param name="RawText">The goal in the student's own words</param>
public record DeclareGoalCommand(int StudentId, string RawText);