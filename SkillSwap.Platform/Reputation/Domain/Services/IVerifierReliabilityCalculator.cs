using SkillSwap.Platform.Reputation.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.Reputation.Domain.Services;

/// <summary>
///     Contract for calculating the reliability of a verifier from an explainable set of rules.
/// </summary>
public interface IVerifierReliabilityCalculator
{
    /// <summary>
    ///     Starts from the full score and discounts every overturned decision and every sanction, never going
    ///     below zero. The number of resolved cases is part of the record but does not change the score.
    /// </summary>
    ReliabilityScore Calculate(int resolvedCasesCount, int overturnedDecisionsCount, int sanctionsCount);
}