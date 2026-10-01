namespace SkillSwap.Platform.LearningPathEngine.Domain.Model.Queries;

/// <summary>
///     Get the current learning path of a student
/// </summary>
/// <param name="StudentId">The student who owns the path</param>
public record GetLearningPathByStudentIdQuery(int StudentId);