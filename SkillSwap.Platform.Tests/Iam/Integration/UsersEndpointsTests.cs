using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SkillSwap.Platform.Iam.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Iam.Integration;

public class UsersEndpointsTests : ApiTestBase
{
    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    // ---------- Authentication of the requests ----------

    [Fact]
    public async Task GetCurrentUser_WithoutToken_Returns401()
    {
        var response = await TestApi.CreateClient().GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidToken_Returns401()
    {
        var client = TestApi.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "not-a-valid-token");

        var response = await client.GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Reading profiles ----------

    [Fact]
    public async Task GetCurrentUser_ReturnsTheFullProfile()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await ana.Client.GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = (await response.Content.ReadFromJsonAsync<UserResource>())!;
        Assert.Equal(ana.Id, user.Id);
        Assert.Equal("ana@upc.edu.pe", user.Email);
    }

    [Fact]
    public async Task GetUserById_OfYourself_IncludesTheEmail()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var json = await ReadJson(await ana.Client.GetAsync($"/api/v1/users/{ana.Id}"));

        Assert.Equal("ana@upc.edu.pe", json.GetProperty("email").GetString());
    }

    [Fact]
    public async Task GetUserById_OfAnotherStudent_OmitsTheEmail()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var bob = await TestApi.RegisterStudentAsync("bob");

        var response = await bob.Client.GetAsync($"/api/v1/users/{ana.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJson(response);
        Assert.Equal("ana", json.GetProperty("username").GetString());
        Assert.False(json.TryGetProperty("email", out _));
    }

    [Fact]
    public async Task GetUserById_AsCoordinator_IncludesTheEmailOfAnyUser()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var coordinator = await TestApi.RegisterCoordinatorAsync("coord");

        var json = await ReadJson(await coordinator.Client.GetAsync($"/api/v1/users/{ana.Id}"));

        Assert.Equal("ana@upc.edu.pe", json.GetProperty("email").GetString());
    }

    [Fact]
    public async Task GetUserById_WithUnknownId_Returns404()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await ana.Client.GetAsync("/api/v1/users/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Updating the bio ----------

    [Fact]
    public async Task UpdateBio_OnYourOwnProfile_Returns200AndPersistsIt()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await ana.Client.PatchAsJsonAsync($"/api/v1/users/{ana.Id}/bio",
            new UpdateUserBioResource("Backend developer"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reloaded = (await ana.Client.GetFromJsonAsync<UserResource>($"/api/v1/users/{ana.Id}"))!;
        Assert.Equal("Backend developer", reloaded.Bio);
    }

    [Fact]
    public async Task UpdateBio_OnAnotherProfile_Returns403AndLeavesItUnchanged()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var bob = await TestApi.RegisterStudentAsync("bob");

        var response = await bob.Client.PatchAsJsonAsync($"/api/v1/users/{ana.Id}/bio",
            new UpdateUserBioResource("Hacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var reloaded = (await ana.Client.GetFromJsonAsync<UserResource>($"/api/v1/users/{ana.Id}"))!;
        Assert.Equal(string.Empty, reloaded.Bio);
    }

    [Fact]
    public async Task UpdateBio_ExceedingTheMaximumLength_Returns400()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await ana.Client.PatchAsJsonAsync($"/api/v1/users/{ana.Id}/bio",
            new UpdateUserBioResource(new string('a', 1001)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBio_WithoutToken_Returns401()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await TestApi.CreateClient().PatchAsJsonAsync($"/api/v1/users/{ana.Id}/bio",
            new UpdateUserBioResource("Anonymous"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}