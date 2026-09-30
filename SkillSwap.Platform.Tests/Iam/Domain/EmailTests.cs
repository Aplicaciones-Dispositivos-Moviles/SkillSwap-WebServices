using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.Iam.Domain;

public class EmailTests
{
    [Theory]
    [InlineData("ana@upc.edu.pe")]
    [InlineData("ana.perez@pucp.edu.pe")]
    [InlineData("u202012345@upc.edu.pe")]
    public void Constructor_WithInstitutionalEmail_CreatesEmail(string value)
    {
        var email = new Email(value);

        Assert.Equal(value, email.Value);
    }

    [Fact]
    public void Constructor_NormalizesToLowercaseAndTrims()
    {
        var email = new Email("  Ana@UPC.EDU.PE ");

        Assert.Equal("ana@upc.edu.pe", email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ana@gmail.com")]
    [InlineData("ana@upc.edu")]
    [InlineData("ana@edu.pe")]
    [InlineData("ana@upc.edu.pe.com")]
    [InlineData("ana upc@upc.edu.pe")]
    [InlineData("upc.edu.pe")]
    public void Constructor_WithInvalidEmail_ThrowsDomainException(string value)
    {
        Assert.Throws<DomainException>(() => new Email(value));
    }

    [Fact]
    public void IsValid_WithNull_ReturnsFalse()
    {
        Assert.False(Email.IsValid(null));
    }

    [Fact]
    public void TwoEmailsWithSameValueAreEqual()
    {
        Assert.Equal(new Email("ana@upc.edu.pe"), new Email("ANA@upc.edu.pe"));
    }
}