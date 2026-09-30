namespace SkillSwap.Platform.Iam.Interfaces.Rest.Resources;

/// <summary>
///     Resource for signing in
/// </summary>
/// <param name="Username">The username</param>
/// <param name="Password">The plain-text password</param>
public record SignInResource(string Username, string Password);