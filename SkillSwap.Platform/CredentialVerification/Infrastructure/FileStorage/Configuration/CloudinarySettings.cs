namespace SkillSwap.Platform.CredentialVerification.Infrastructure.FileStorage.Configuration;

/// <summary>
///     Cloudinary credentials, bound from the "Cloudinary" configuration section.
///     Never commit real values: use appsettings.Development.json locally and environment
///     variables (Cloudinary__CloudName, Cloudinary__ApiKey, Cloudinary__ApiSecret) in production.
/// </summary>
public class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}