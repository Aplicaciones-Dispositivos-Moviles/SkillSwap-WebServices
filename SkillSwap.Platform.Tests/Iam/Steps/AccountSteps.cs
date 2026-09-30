using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Reqnroll;
using SkillSwap.Platform.Iam.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Iam.Steps;

[Binding]
public class AccountSteps(ApiScenarioContext context)
{
    private const string SignUpUrl = "/api/v1/authentication/sign-up";
    private const string SignInUrl = "/api/v1/authentication/sign-in";

    private async Task<HttpResponseMessage> PostAsync(string url, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        if (context.AcceptLanguage is not null) request.Headers.AcceptLanguage.ParseAdd(context.AcceptLanguage);
        return await TestApi.CreateClient().SendAsync(request);
    }

    // ---------- Given ----------

    [Given("an account exists for {string} with the email {string}")]
    public async Task GivenAnAccountExistsWithTheEmail(string username, string email)
    {
        var response = await TestApi.CreateClient().PostAsJsonAsync(SignUpUrl,
            new SignUpResource(username, email, TestApi.DefaultPassword));
        response.EnsureSuccessStatusCode();
    }

    [Given("an account exists for {string}")]
    public async Task GivenAnAccountExists(string username)
    {
        await GivenAnAccountExistsWithTheEmail(username, TestApi.EmailFor(username));
    }

    [Given("the client prefers the language {string}")]
    public void GivenTheClientPrefersTheLanguage(string language)
    {
        context.AcceptLanguage = language;
    }

    // ---------- When ----------

    [When("I sign up as {string} with the email {string} and the password {string}")]
    public async Task WhenISignUp(string username, string email, string password)
    {
        context.Response = await PostAsync(SignUpUrl, new SignUpResource(username, email, password));
    }

    [When("I sign up as {string} with the email {string}, the password {string} and the role {string}")]
    public async Task WhenISignUpSendingARole(string username, string email, string password, string role)
    {
        context.Response = await PostAsync(SignUpUrl, new { username, email, password, role });
    }

    [When("I sign in as {string} with the password {string}")]
    public async Task WhenISignIn(string username, string password)
    {
        context.Response = await PostAsync(SignInUrl, new SignInResource(username, password));
    }

    // ---------- Then ----------

    [Then("the response status is {int}")]
    public void ThenTheResponseStatusIs(int status)
    {
        Assert.Equal(status, (int)context.Response!.StatusCode);
    }

    [Then("the error is {string}")]
    public async Task ThenTheErrorIs(string error)
    {
        var problem = await context.ReadBodyAsync<ProblemDetails>();
        Assert.Equal(error, problem.Title);
    }

    [Then("the error message is {string}")]
    public async Task ThenTheErrorMessageIs(string message)
    {
        var problem = await context.ReadBodyAsync<ProblemDetails>();
        Assert.Equal(message, problem.Detail);
    }

    [Then("the created account has the role {string}")]
    public async Task ThenTheCreatedAccountHasTheRole(string role)
    {
        var user = await context.ReadBodyAsync<UserResource>();
        Assert.Equal(role, user.Role);
    }

    [Then("the response contains an access token")]
    public async Task ThenTheResponseContainsAnAccessToken()
    {
        var authenticated = await context.ReadBodyAsync<AuthenticatedUserResource>();
        Assert.False(string.IsNullOrWhiteSpace(authenticated.Token));
    }

    [Then("the token grants access to the profile of {string}")]
    public async Task ThenTheTokenGrantsAccessToTheProfileOf(string username)
    {
        var authenticated = await context.ReadBodyAsync<AuthenticatedUserResource>();

        using var client = TestApi.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", authenticated.Token);
        var profile = await client.GetAsync("/api/v1/users/me");

        Assert.True(profile.IsSuccessStatusCode);
        var user = await profile.Content.ReadFromJsonAsync<UserResource>();
        Assert.Equal(username.ToLowerInvariant(), user!.Username);
    }
}