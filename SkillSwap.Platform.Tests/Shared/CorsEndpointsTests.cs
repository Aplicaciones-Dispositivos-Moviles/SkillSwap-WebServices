using System.Net;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Shared;

public class CorsEndpointsTests
{
    [Fact]
    public async Task Preflight_WithoutAToken_IsAnsweredBeforeTheAuthorizationMiddleware()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/learning-paths");
        request.Headers.Add("Origin", "https://landing.example.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        var response = await TestApi.CreateClient().SendAsync(request);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("*", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Request_WithAnOrigin_ReceivesTheAllowOriginHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://landing.example.com");

        var response = await TestApi.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("*", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}