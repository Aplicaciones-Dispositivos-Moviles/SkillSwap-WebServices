using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SkillSwap.Platform.Shared.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Shared;

public class HealthEndpointsTests
{
    [Fact]
    public async Task Health_WithoutAToken_Returns200AndHealthy()
    {
        var response = await TestApi.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var health = (await response.Content.ReadFromJsonAsync<HealthResource>())!;
        Assert.Equal("Healthy", health.Status);
        Assert.Equal(DateTimeKind.Utc, health.Timestamp.Kind);
    }

    [Fact]
    public async Task Root_RedirectsToTheApiDocumentationWithoutAToken()
    {
        var client = TestApi.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task SwaggerDocument_DocumentsTheHealthAndAssessmentEndpoints()
    {
        var response = await TestApi.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"/health\"", document);
        Assert.Contains("\"/api/v1/assessment-attempts\"", document);
        Assert.Contains("\"/api/v1/verification-cases/{caseId}/evidence\"", document);
        Assert.Contains("\"/api/v1/verifier-profiles/me/availability\"", document);
    }
}