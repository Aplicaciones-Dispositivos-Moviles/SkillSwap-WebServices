using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Platform.Shared.Infrastructure.Cors;

namespace SkillSwap.Platform.Tests.Shared;

public class CorsConfigurationExtensionsTests
{
    private static IConfiguration Configuration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => (string?)v.Value))
            .Build();
    }

    private static async Task<CorsPolicy> PolicyAsync(IConfiguration configuration)
    {
        var services = new ServiceCollection().AddConfiguredCors(configuration).BuildServiceProvider();
        var provider = services.GetRequiredService<ICorsPolicyProvider>();
        return (await provider.GetPolicyAsync(new DefaultHttpContext(), CorsConfigurationExtensions.PolicyName))!;
    }

    [Fact]
    public async Task WithoutOrigins_AllowsAnyOrigin()
    {
        var policy = await PolicyAsync(Configuration());

        Assert.True(policy.AllowAnyOrigin);
        Assert.True(policy.AllowAnyMethod);
        Assert.True(policy.AllowAnyHeader);
    }

    [Fact]
    public async Task WithAnIndexedList_AllowsOnlyThoseOrigins()
    {
        var policy = await PolicyAsync(Configuration(
            ("Cors:AllowedOrigins:0", "https://landing.example.com"),
            ("Cors:AllowedOrigins:1", "https://admin.example.com")));

        Assert.False(policy.AllowAnyOrigin);
        Assert.Equal(["https://landing.example.com", "https://admin.example.com"], policy.Origins);
        Assert.True(policy.AllowAnyMethod);
        Assert.True(policy.AllowAnyHeader);
    }

    [Fact]
    public async Task WithACommaSeparatedValue_SplitsIt()
    {
        var policy = await PolicyAsync(Configuration(
            ("Cors:AllowedOrigins", "https://landing.example.com, https://admin.example.com")));

        Assert.False(policy.AllowAnyOrigin);
        Assert.Equal(["https://landing.example.com", "https://admin.example.com"], policy.Origins);
    }

    [Fact]
    public void ReadOrigins_TrimsTrailingSlashesBlanksAndDuplicates()
    {
        var origins = CorsConfigurationExtensions.ReadOrigins(Configuration(
            ("Cors:AllowedOrigins:0", "https://landing.example.com/"),
            ("Cors:AllowedOrigins:1", "HTTPS://LANDING.EXAMPLE.COM"),
            ("Cors:AllowedOrigins:2", "  "),
            ("Cors:AllowedOrigins:3", "https://admin.example.com")));

        Assert.Equal(["https://landing.example.com", "https://admin.example.com"], origins);
    }

    [Fact]
    public async Task WithOnlyBlankValues_FallsBackToAnyOrigin()
    {
        var policy = await PolicyAsync(Configuration(("Cors:AllowedOrigins:0", " ")));

        Assert.True(policy.AllowAnyOrigin);
    }
}