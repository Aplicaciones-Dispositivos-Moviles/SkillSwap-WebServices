using SkillSwap.Platform.Reputation.Domain.Services;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.Reputation.Domain;

public class ReputationCalculatorsTests
{
    private readonly EmployabilityScoreCalculator _employability = new();
    private readonly VerifierReliabilityCalculator _reliability = new();

    // ---------- Reliability ----------

    [Theory]
    [InlineData(0, 0, 0, 100)]
    [InlineData(40, 0, 0, 100)]
    [InlineData(5, 1, 0, 85)]
    [InlineData(5, 2, 0, 70)]
    [InlineData(5, 0, 1, 75)]
    [InlineData(5, 1, 1, 60)]
    [InlineData(5, 2, 2, 20)]
    public void Reliability_StartsFromFullAndDiscountsOverturnsAndSanctions(int resolved, int overturned,
        int sanctions, int expected)
    {
        Assert.Equal(expected, _reliability.Calculate(resolved, overturned, sanctions).Value);
    }

    [Theory]
    [InlineData(0, 7, 0)]
    [InlineData(0, 0, 4)]
    [InlineData(0, 100, 100)]
    [InlineData(0, int.MaxValue, int.MaxValue)]
    public void Reliability_NeverDropsBelowZero(int resolved, int overturned, int sanctions)
    {
        Assert.Equal(0, _reliability.Calculate(resolved, overturned, sanctions).Value);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void Reliability_WithANegativeCounter_ThrowsDomainException(int resolved, int overturned, int sanctions)
    {
        Assert.Throws<DomainException>(() => _reliability.Calculate(resolved, overturned, sanctions));
    }

    // ---------- Employability ----------

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 10)]
    [InlineData(5, 50)]
    [InlineData(10, 100)]
    [InlineData(15, 100)]
    [InlineData(int.MaxValue, 100)]
    public void Employability_GivesTenPointsPerSkillUpToOneHundred(int skills, int expected)
    {
        Assert.Equal(expected, _employability.Calculate(skills).Value);
    }

    [Fact]
    public void Employability_WithANegativeCount_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => _employability.Calculate(-1));
    }
}