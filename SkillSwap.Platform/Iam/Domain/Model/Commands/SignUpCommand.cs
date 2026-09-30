using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.Iam.Domain.Model.Commands;

/// <summary>
///     Sign up command
/// </summary>
/// <param name="Username">The desired username</param>
/// <param name="Email">The institutional email (.edu.pe)</param>
/// <param name="Password">The plain-text password to hash and store</param>
/// <param name="Role">The role of the new account</param>
public record SignUpCommand(string Username, string Email, string Password, UserRole Role);