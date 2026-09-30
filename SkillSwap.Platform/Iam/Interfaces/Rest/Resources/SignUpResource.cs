namespace SkillSwap.Platform.Iam.Interfaces.Rest.Resources;

/// <summary>
///     Resource for signing up a new account
/// </summary>
/// <param name="Username">The desired username</param>
/// <param name="Email">The institutional email (.edu.pe)</param>
/// <param name="Password">The plain-text password (8 to 72 characters)</param>
public record SignUpResource(string Username, string Email, string Password);