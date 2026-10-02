using SkillSwap.Platform.Reputation.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Reputation.Domain.Services;

/// <summary>
///     Reliability = 100 - 15 per overturned decision - 25 per sanction, with a minimum of 0.
/// </summary>
public class VerifierReliabilityCalculator : IVerifierReliabilityCalculator
{
    public const int OverturnPenalty = 15;
    public const int SanctionPenalty = 25;

    /// <inheritdoc />
    public ReliabilityScore Calculate(int resolvedCasesCount, int overturnedDecisionsCount, int sanctionsCount)
    {
        if (resolvedCasesCount < 0 || overturnedDecisionsCount < 0 || sanctionsCount < 0)
            throw new DomainException("The counters cannot be negative.");

        var score = ReliabilityScore.Max
                    - (long)overturnedDecisionsCount * OverturnPenalty
                    - (long)sanctionsCount * SanctionPenalty;
        return new ReliabilityScore((int)Math.Max(ReliabilityScore.Min, score));
    }
}