namespace SkillSwap.Platform.Iam.Interfaces.Rest.Resources;

/// <summary>
///     Public view of a user, visible to any authenticated user. Excludes private data such as the email.
/// </summary>
/// <param name="Id">The unique identifier of the user</param>
/// <param name="Username">The username</param>
/// <param name="Role">The account role: Student or Coordinator</param>
/// <param name="IsVerified">Whether the institutional validation has been completed</param>
/// <param name="Bio">The profile description</param>
public record PublicUserResource(int Id, string Username, string Role, bool IsVerified, string Bio);