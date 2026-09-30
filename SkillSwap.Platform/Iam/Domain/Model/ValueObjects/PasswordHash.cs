using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Iam.Domain.Model.ValueObjects;

/// <summary>
///     Encrypted representation of a password. The plain-text value never travels
///     beyond the infrastructure layer.
/// </summary>
public sealed record PasswordHash
{
    public PasswordHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("The password hash cannot be empty.");
        Value = value;
    }

    public string Value { get; }
}