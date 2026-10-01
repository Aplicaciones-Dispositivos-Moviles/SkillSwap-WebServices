using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;

/// <summary>
///     Explainable, rule-based result of a certificate risk evaluation. The level is always
///     derived from the score: 0-19 low risk, 20-49 review, 50+ high risk.
/// </summary>
public sealed record RiskAssessment
{
    public const int ReviewThreshold = 20;
    public const int HighRiskThreshold = 50;

    public RiskAssessment(int score)
    {
        if (score < 0)
            throw new DomainException("The risk score cannot be negative.");

        Score = score;
        Level = score >= HighRiskThreshold ? RiskLevel.HighRisk
            : score >= ReviewThreshold ? RiskLevel.Review
            : RiskLevel.LowRisk;
    }

    public int Score { get; }
    public RiskLevel Level { get; }
}