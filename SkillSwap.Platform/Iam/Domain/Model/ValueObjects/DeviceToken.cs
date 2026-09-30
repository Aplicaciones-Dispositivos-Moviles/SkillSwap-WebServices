using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Iam.Domain.Model.ValueObjects;

/// <summary>
///     Opaque identifier of the user's mobile device. Reserved as an extension point for a
///     future push-notification integration (provider still to be defined).
/// </summary>
public sealed record DeviceToken
{
    public DeviceToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("The device token cannot be empty.");
        Value = value.Trim();
    }

    public string Value { get; }
}