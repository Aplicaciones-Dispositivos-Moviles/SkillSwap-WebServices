namespace SkillSwap.Platform.Iam.Domain.Model.Commands;

/// <summary>
///     Sign in command
/// </summary>
/// <param name="Username">The username</param>
/// <param name="Password">The plain-text password</param>
public record SignInCommand(string Username, string Password);