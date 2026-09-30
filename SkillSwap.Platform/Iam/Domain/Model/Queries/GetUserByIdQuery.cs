namespace SkillSwap.Platform.Iam.Domain.Model.Queries;

/// <summary>
///     Get user by id query
/// </summary>
/// <param name="UserId">The unique identifier of the user</param>
public record GetUserByIdQuery(int UserId);