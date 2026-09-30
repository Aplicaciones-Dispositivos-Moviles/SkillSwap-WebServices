using System.Text.RegularExpressions;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Iam.Domain.Model.ValueObjects;

/// <summary>
///     Institutional email address. Only addresses from the .edu.pe domain are accepted.
///     Stored in lowercase.
/// </summary>
public sealed partial record Email
{
    public const string InstitutionalSuffix = ".edu.pe";

    public Email(string value)
    {
        if (!IsValid(value))
            throw new DomainException("The email must be a valid institutional (.edu.pe) email address.");
        Value = Normalize(value);
    }

    public string Value { get; }

    public static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && EmailPattern().IsMatch(Normalize(value));
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.edu\.pe$")]
    private static partial Regex EmailPattern();

    public override string ToString()
    {
        return Value;
    }
}