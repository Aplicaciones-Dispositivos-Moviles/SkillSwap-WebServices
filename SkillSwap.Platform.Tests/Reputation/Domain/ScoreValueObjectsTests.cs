using SkillSwap.Platform.Reputation.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.Reputation.Domain;

public class ScoreValueObjectsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(55)]
    [InlineData(100)]
    public void ReliabilityScore_WithAValueInRange_KeepsIt(int value)
    {
        Assert.Equal(value, new ReliabilityScore(value).Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ReliabilityScore_OutOfRange_ThrowsDomainException(int value)
    {
        Assert.Throws<DomainException>(() => new ReliabilityScore(value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(40)]
    [InlineData(100)]
    public void EmployabilityScore_WithAValueInRange_KeepsIt(int value)
    {
        Assert.Equal(value, new EmployabilityScore(value).Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void EmployabilityScore_OutOfRange_ThrowsDomainException(int value)
    {
        Assert.Throws<DomainException>(() => new EmployabilityScore(value));
    }
}