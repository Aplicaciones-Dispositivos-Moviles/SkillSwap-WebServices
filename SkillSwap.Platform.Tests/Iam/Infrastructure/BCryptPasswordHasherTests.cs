using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;

namespace SkillSwap.Platform.Tests.Iam.Infrastructure;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_DoesNotStoreThePlainText()
    {
        var hash = _hasher.HashPassword("password123");

        Assert.NotEqual("password123", hash.Value);
        Assert.StartsWith("$2", hash.Value);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.HashPassword("password123");

        Assert.True(_hasher.VerifyPassword("password123", hash));
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.HashPassword("password123");

        Assert.False(_hasher.VerifyPassword("password124", hash));
    }

    [Fact]
    public void HashPassword_UsesRandomSaltPerHash()
    {
        Assert.NotEqual(_hasher.HashPassword("password123"), _hasher.HashPassword("password123"));
    }

    [Fact]
    public void VerifyPassword_WithMalformedHash_Throws()
    {
        Assert.ThrowsAny<Exception>(() => _hasher.VerifyPassword("password123", new PasswordHash("not-a-hash")));
    }
}