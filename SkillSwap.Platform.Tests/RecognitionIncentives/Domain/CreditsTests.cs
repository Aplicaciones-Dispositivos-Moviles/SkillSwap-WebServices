using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.RecognitionIncentives.Domain;

public class CreditsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50)]
    public void Constructor_WithANonNegativeValue_KeepsIt(int value)
    {
        Assert.Equal(value, new Credits(value).Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Constructor_WithANegativeValue_ThrowsDomainException(int value)
    {
        Assert.Throws<DomainException>(() => new Credits(value));
    }

    [Fact]
    public void IsPositive_IsFalseOnlyForZero()
    {
        Assert.False(new Credits(0).IsPositive);
        Assert.True(new Credits(1).IsPositive);
    }

    [Fact]
    public void TwoAmountsWithTheSameValue_AreEqual()
    {
        Assert.Equal(new Credits(10), new Credits(10));
        Assert.NotEqual(new Credits(10), new Credits(20));
    }
}