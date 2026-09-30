using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Iam.Domain.Model.ValueObjects;

/// <summary>
///     Unique text identifier used to access the system. Stored in lowercase.
/// </summary>
public sealed record Username
{
    public const int MinLength = 3;
    public const int MaxLength = 100;

    public Username(string value)
    {
        if (!IsValid(value))
            throw new DomainException(
                $"Username must be {MinLength}-{MaxLength} characters long and contain no whitespace.");
        Value = value.Trim().ToLowerInvariant();
    }

    public string Value { get; }

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var trimmed = value.Trim();
        return trimmed.Length is >= MinLength and <= MaxLength && !trimmed.Any(char.IsWhiteSpace);
    }

    public override string ToString()
    {
        return Value;
    }
}