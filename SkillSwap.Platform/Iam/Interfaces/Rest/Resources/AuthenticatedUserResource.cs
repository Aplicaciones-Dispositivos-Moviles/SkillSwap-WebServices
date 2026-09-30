namespace SkillSwap.Platform.Iam.Interfaces.Rest.Resources;

/// <summary>
///     Result of a successful sign-in: basic user information plus the JWT
/// </summary>
/// <param name="Id">The unique identifier of the user</param>
/// <param name="Username">The username</param>
/// <param name="Email">The institutional email</param>
/// <param name="Role">The account role: Student or Coordinator</param>
/// <param name="IsVerified">Whether the institutional validation has been completed</param>
/// <param name="Token">The JWT to send as a Bearer token</param>
public record AuthenticatedUserResource(
    int Id,
    string Username,
    string Email,
    string Role,
    bool IsVerified,
    string Token);