using System.Net.Http.Json;
using System.Text.Json;
using Reqnroll;
using SkillSwap.Platform.Iam.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Iam.Steps;

[Binding]
public class ProfileSteps(ApiScenarioContext context)
{
    [Given("a signed-in student {string}")]
    public async Task GivenASignedInStudent(string username)
    {
        context.Users[username] = await TestApi.RegisterStudentAsync(username);
    }

    [When("{string} requests the profile of {string}")]
    public async Task WhenRequestsTheProfileOf(string actor, string target)
    {
        context.Response = await context.Users[actor].Client.GetAsync($"/api/v1/users/{context.Users[target].Id}");
    }

    [When("{string} updates the bio of {string} to {string}")]
    public async Task WhenUpdatesTheBioOf(string actor, string target, string bio)
    {
        context.Response = await context.Users[actor].Client.PatchAsJsonAsync(
            $"/api/v1/users/{context.Users[target].Id}/bio", new UpdateUserBioResource(bio));
    }

    [Then("the bio of {string} is {string}")]
    public async Task ThenTheBioOfIs(string username, string expected)
    {
        Assert.Equal(expected, await ReadBioAsync(username));
    }

    [Then("the bio of {string} is empty")]
    public async Task ThenTheBioOfIsEmpty(string username)
    {
        Assert.Equal(string.Empty, await ReadBioAsync(username));
    }

    [Then("the profile exposes the username {string}")]
    public async Task ThenTheProfileExposesTheUsername(string username)
    {
        using var json = JsonDocument.Parse(await context.Response!.Content.ReadAsStringAsync());
        Assert.Equal(username, json.RootElement.GetProperty("username").GetString());
    }

    [Then("the profile does not expose the email")]
    public async Task ThenTheProfileDoesNotExposeTheEmail()
    {
        using var json = JsonDocument.Parse(await context.Response!.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.TryGetProperty("email", out _));
    }

    private async Task<string> ReadBioAsync(string username)
    {
        var owner = context.Users[username];
        var user = await owner.Client.GetFromJsonAsync<UserResource>($"/api/v1/users/{owner.Id}");
        return user!.Bio;
    }
}