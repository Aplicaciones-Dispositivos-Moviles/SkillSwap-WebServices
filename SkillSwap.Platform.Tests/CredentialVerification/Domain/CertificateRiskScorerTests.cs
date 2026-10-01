using SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;
using SkillSwap.Platform.CredentialVerification.Domain.Services;

namespace SkillSwap.Platform.Tests.CredentialVerification.Domain;

public class CertificateRiskScorerTests
{
    private readonly CertificateRiskScorer _scorer = new();

    [Theory]
    //   number code  hash   ocr    score level
    [InlineData(false, false, false, false, 0, RiskLevel.LowRisk)]
    [InlineData(false, false, true, false, 50, RiskLevel.HighRisk)]
    [InlineData(false, false, false, true, 15, RiskLevel.LowRisk)]
    [InlineData(false, false, true, true, 65, RiskLevel.HighRisk)]
    [InlineData(true, false, false, false, 30, RiskLevel.Review)]
    [InlineData(false, true, false, false, 30, RiskLevel.Review)]
    [InlineData(true, false, false, true, 45, RiskLevel.Review)]
    [InlineData(true, true, false, false, 60, RiskLevel.HighRisk)]
    [InlineData(true, false, true, true, 95, RiskLevel.HighRisk)]
    [InlineData(true, true, true, true, 125, RiskLevel.HighRisk)]
    public void CalculateRisk_AddsThePointsOfEachRule(bool number, bool code, bool hash, bool ocr, int score,
        RiskLevel level)
    {
        var assessment = _scorer.CalculateRisk(number, code, hash, ocr);

        Assert.Equal(score, assessment.Score);
        Assert.Equal(level, assessment.Level);
    }

    [Fact]
    public void DuplicateFile_AloneIsHighRisk()
    {
        Assert.Equal(RiskLevel.HighRisk, _scorer.CalculateRisk(false, false, true, false).Level);
    }
}