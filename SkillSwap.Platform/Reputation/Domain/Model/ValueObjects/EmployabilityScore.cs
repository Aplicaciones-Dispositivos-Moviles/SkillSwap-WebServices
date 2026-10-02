using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Reputation.Domain.Model.ValueObjects;

/// <summary>
///     Employability demonstrated by a student, from 0 to 100.
/// </summary>
public sealed record EmployabilityScore
{
    public const int Min = 0;
    public const int Max = 100;

    public EmployabilityScore(int value)
    {
        if (value < Min || value > Max)
            throw new DomainException($"The employability score must be between {Min} and {Max}.");

        Value = value;
    }

    public int Value { get; }
}