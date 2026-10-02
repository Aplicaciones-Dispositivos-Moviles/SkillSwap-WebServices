using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;

/// <summary>
///     An amount of SkillCredits, internal non-monetary credits. It is never negative.
/// </summary>
public sealed record Credits
{
    public Credits(int value)
    {
        if (value < 0)
            throw new DomainException("The amount of credits cannot be negative.");

        Value = value;
    }

    public int Value { get; }

    public bool IsPositive => Value > 0;
}