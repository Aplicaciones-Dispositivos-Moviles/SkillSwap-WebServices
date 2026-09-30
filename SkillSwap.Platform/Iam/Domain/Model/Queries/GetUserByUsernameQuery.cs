namespace SkillSwap.Platform.Iam.Domain.Model.Queries;

/// <summary>
///     Get user by username query
/// </summary>
/// <param name="Username">The username to search for</param>
public record GetUserByUsernameQuery(string Username);