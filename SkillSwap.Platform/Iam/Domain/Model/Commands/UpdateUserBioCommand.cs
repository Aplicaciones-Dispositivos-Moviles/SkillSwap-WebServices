namespace SkillSwap.Platform.Iam.Domain.Model.Commands;

/// <summary>
///     Update user bio command
/// </summary>
/// <param name="UserId">The id of the profile being updated</param>
/// <param name="Bio">The new bio text</param>
/// <param name="ActorUserId">The id of the authenticated user performing the update</param>
public record UpdateUserBioCommand(int UserId, string Bio, int ActorUserId);