using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Reputation.Domain.Model.ValueObjects;

/// <summary>
///     Reliability of a verifier, from 0 to 100.
/// </summary>
public sealed record ReliabilityScore
{
    public const int Min = 0;
    public const int Max = 100;

    public ReliabilityScore(int value)
    {
        if (value < Min || value > Max)
            throw new DomainException($"The reliability score must be between {Min} and {Max}.");

        Value = value;
    }

    public int Value { get; }
}