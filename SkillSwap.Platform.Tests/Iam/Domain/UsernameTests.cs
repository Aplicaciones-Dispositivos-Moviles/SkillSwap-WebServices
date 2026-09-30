using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.Iam.Domain;

public class UsernameTests
{
    [Fact]
    public void Constructor_NormalizesToLowercaseAndTrims()
    {
        var username = new Username("  Ana_Perez ");

        Assert.Equal("ana_perez", username.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("with space")]
    public void Constructor_WithInvalidUsername_ThrowsDomainException(string value)
    {
        Assert.Throws<DomainException>(() => new Username(value));
    }

    [Fact]
    public void Constructor_WithMoreThan100Characters_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new Username(new string('a', 101)));
    }

    [Fact]
    public void Constructor_WithExactly100Characters_IsAccepted()
    {
        var username = new Username(new string('a', 100));

        Assert.Equal(100, username.Value.Length);
    }

    [Fact]
    public void UsernamesDifferingOnlyInCaseAreEqual()
    {
        Assert.Equal(new Username("Ana"), new Username("ana"));
    }
}