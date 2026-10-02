using SkillSwap.Platform.Reputation.Domain.Model.ValueObjects;
using SkillSwap.Platform.Reputation.Domain.Services;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Reputation.Domain.Model.Aggregates;

/// <summary>
///     VerifierReliability aggregate root
/// </summary>
/// <remarks>
///     The accumulated reliability of a verifier. It only changes through events of other bounded contexts, never
///     because a user rates another, and every change recalculates the score so it always agrees with the counters.
/// </remarks>
public class VerifierReliability
{
    /// <summary>
    ///     Parameterless constructor required by EF Core.
    /// </summary>
    protected VerifierReliability()
    {
        Score = null!;
    }

    public VerifierReliability(int verifierUserId)
    {
        if (verifierUserId <= 0)
            throw new DomainException("The reliability must belong to a valid user.");

        VerifierUserId = verifierUserId;
        Score = new ReliabilityScore(ReliabilityScore.Max);
        UpdatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int VerifierUserId { get; private set; }
    public int ResolvedCasesCount { get; private set; }
    public int OverturnedDecisionsCount { get; private set; }
    public int SanctionsCount { get; private set; }
    public ReliabilityScore Score { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    ///     The verifier resolved a case, approving or rejecting it.
    /// </summary>
    public VerifierReliability RecordResolution(IVerifierReliabilityCalculator calculator)
    {
        ResolvedCasesCount++;
        return Recalculate(calculator);
    }

    /// <summary>
    ///     A decision of the verifier was overturned after a dispute.
    /// </summary>
    public VerifierReliability RecordOverturn(IVerifierReliabilityCalculator calculator)
    {
        OverturnedDecisionsCount++;
        return Recalculate(calculator);
    }

    /// <summary>
    ///     A sanction was applied to the verifier's account, the heaviest penalty of the model.
    /// </summary>
    public VerifierReliability ApplySanction(IVerifierReliabilityCalculator calculator)
    {
        SanctionsCount++;
        return Recalculate(calculator);
    }

    private VerifierReliability Recalculate(IVerifierReliabilityCalculator calculator)
    {
        ArgumentNullException.ThrowIfNull(calculator);

        Score = calculator.Calculate(ResolvedCasesCount, OverturnedDecisionsCount, SanctionsCount);
        UpdatedAt = DateTime.UtcNow;
        return this;
    }
}