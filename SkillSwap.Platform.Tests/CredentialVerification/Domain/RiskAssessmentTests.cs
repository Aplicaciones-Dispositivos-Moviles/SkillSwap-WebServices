using SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.CredentialVerification.Domain;

public class RiskAssessmentTests
{
    [Theory]
    [InlineData(0, RiskLevel.LowRisk)]
    [InlineData(19, RiskLevel.LowRisk)]
    [InlineData(20, RiskLevel.Review)]
    [InlineData(49, RiskLevel.Review)]
    [InlineData(50, RiskLevel.HighRisk)]
    [InlineData(85, RiskLevel.HighRisk)]
    public void Constructor_DerivesTheLevelFromTheScore(int score, RiskLevel expected)
    {
        var assessment = new RiskAssessment(score);

        Assert.Equal(score, assessment.Score);
        Assert.Equal(expected, assessment.Level);
    }

    [Fact]
    public void Constructor_WithNegativeScore_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new RiskAssessment(-1));
    }
}