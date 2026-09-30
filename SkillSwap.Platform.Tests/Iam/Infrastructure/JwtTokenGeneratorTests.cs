using Microsoft.Extensions.Options;
using SkillSwap.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using SkillSwap.Platform.Iam.Infrastructure.Tokens.Jwt.Services;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Iam.Infrastructure;

public class JwtTokenGeneratorTests
{
    private const string Secret = "test-secret-with-at-least-32-characters-long!";

    private static JwtTokenGenerator CreateGenerator(string secret = Secret)
    {
        return new JwtTokenGenerator(Options.Create(new TokenSettings { Secret = secret }));
    }

    [Fact]
    public async Task ValidateToken_WithTokenFromSameSecret_ReturnsTheUserId()
    {
        var generator = CreateGenerator();
        var token = generator.GenerateToken(TestData.NewUser(7));

        var userId = await generator.ValidateToken(token);

        Assert.Equal(7, userId);
    }

    [Fact]
    public async Task ValidateToken_WithTokenSignedByAnotherSecret_ReturnsNull()
    {
        var token = CreateGenerator("another-secret-with-at-least-32-characters!")
            .GenerateToken(TestData.NewUser(7));

        Assert.Null(await CreateGenerator().ValidateToken(token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-jwt")]
    public async Task ValidateToken_WithInvalidToken_ReturnsNull(string token)
    {
        Assert.Null(await CreateGenerator().ValidateToken(token));
    }

    [Fact]
    public async Task ValidateToken_WithTamperedToken_ReturnsNull()
    {
        var generator = CreateGenerator();
        var token = generator.GenerateToken(TestData.NewUser(7));
        var tampered = token[..^3] + (token.EndsWith("AAA") ? "BBB" : "AAA");

        Assert.Null(await generator.ValidateToken(tampered));
    }

    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    public void Constructor_WithMissingOrShortSecret_Throws(string secret)
    {
        Assert.Throws<InvalidOperationException>(() => CreateGenerator(secret));
    }
}