namespace SkillSwap.Platform.Iam.Interfaces.Rest.Resources;

/// <summary>
///     User resource for REST API
/// </summary>
/// <param name="Id">The unique identifier of the user</param>
/// <param name="Username">The username</param>
/// <param name="Email">The institutional email</param>
/// <param name="Role">The account role: Student or Coordinator</param>
/// <param name="IsVerified">Whether the institutional validation has been completed</param>
/// <param name="Bio">The profile description</param>
public record UserResource(int Id, string Username, string Email, string Role, bool IsVerified, string Bio);