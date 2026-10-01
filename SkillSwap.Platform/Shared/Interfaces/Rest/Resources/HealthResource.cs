namespace SkillSwap.Platform.Shared.Interfaces.Rest.Resources;

/// <summary>
///     Health resource for REST API
/// </summary>
/// <param name="Status">Healthy while the service is running</param>
/// <param name="Timestamp">When the check was answered (UTC)</param>
public record HealthResource(string Status, DateTime Timestamp);