using SkillSwap.Platform.Reputation.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.Reputation.Domain.Services;

/// <summary>
///     Contract for calculating the employability of a student from the skills they certified.
/// </summary>
public interface IEmployabilityScoreCalculator
{
    EmployabilityScore Calculate(int verifiedSkillsCount);
}