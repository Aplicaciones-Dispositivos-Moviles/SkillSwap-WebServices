namespace SkillSwap.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;

/// <summary>
///     JWT settings, bound from the "TokenSettings" configuration section
/// </summary>
public class TokenSettings
{
    /// <summary>
    ///     Shared secret used to sign tokens (at least 32 characters)
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    ///     Token lifetime in days
    /// </summary>
    public int ExpirationDays { get; set; } = 7;
}