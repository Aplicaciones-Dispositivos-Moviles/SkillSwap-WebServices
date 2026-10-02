using SkillSwap.Platform.Reputation.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Reputation.Domain.Services;

/// <summary>
///     Employability = 10 points per certified skill, with a maximum of 100.
/// </summary>
public class EmployabilityScoreCalculator : IEmployabilityScoreCalculator
{
    public const int PointsPerSkill = 10;

    /// <inheritdoc />
    public EmployabilityScore Calculate(int verifiedSkillsCount)
    {
        if (verifiedSkillsCount < 0)
            throw new DomainException("The number of verified skills cannot be negative.");

        var score = Math.Min(EmployabilityScore.Max, (long)verifiedSkillsCount * PointsPerSkill);
        return new EmployabilityScore((int)score);
    }
}