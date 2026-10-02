namespace SkillSwap.Platform.Reputation.Domain.Model.Queries;

/// <summary>
///     Get the employability of a student
/// </summary>
/// <param name="StudentId">The student</param>
public record GetStudentEmployabilityByStudentIdQuery(int StudentId);