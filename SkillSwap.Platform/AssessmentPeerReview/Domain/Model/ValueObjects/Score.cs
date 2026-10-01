using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;

/// <summary>
///     Result of grading an attempt: the correct answers over the total number of questions.
/// </summary>
public sealed record Score
{
    public Score(int value, int total)
    {
        if (total <= 0)
            throw new DomainException("The total score must be positive.");
        if (value < 0 || value > total)
            throw new DomainException("The score must be between 0 and the total.");

        Value = value;
        Total = total;
    }

    public int Value { get; }
    public int Total { get; }
}