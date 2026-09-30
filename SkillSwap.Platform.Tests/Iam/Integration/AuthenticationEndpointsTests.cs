using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Interfaces.Rest.Resources;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Iam.Integration;

public class AuthenticationEndpointsTests : ApiTestBase
{
    private const string SignUpUrl = "/api/v1/authentication/sign-up";
    private const string SignInUrl = "/api/v1/authentication/sign-in";

    private readonly HttpClient _client = TestApi.CreateClient();

    private Task<HttpResponseMessage> SignUp(string username, string email, string password = TestApi.DefaultPassword)
    {
        return _client.PostAsJsonAsync(SignUpUrl, new SignUpResource(username, email, password));
    }

    private Task<HttpResponseMessage> SignIn(string username, string password = TestApi.DefaultPassword)
    {
        return _client.PostAsJsonAsync(SignInUrl, new SignInResource(username, password));
    }

    private static async Task<ProblemDetails> ReadProblem(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
    }

    // ---------- Sign up ----------

    [Fact]
    public async Task SignUp_WithValidData_Returns201AndTheProfileWithoutSecrets()
    {
        var response = await SignUp("Ana", "Ana@UPC.edu.pe");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = (await response.Content.ReadFromJsonAsync<UserResource>())!;
        Assert.Equal("ana", user.Username);
        Assert.Equal("ana@upc.edu.pe", user.Email);
        Assert.Equal("Student", user.Role);
        Assert.False(user.IsVerified);
        Assert.Equal($"/api/v1/users/{user.Id}", response.Headers.Location?.OriginalString);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SignUp_StoresABCryptHashAndNeverThePlainPassword()
    {
        await SignUp("ana", "ana@upc.edu.pe");

        using var scope = TestApi.CreateScope();
        var stored = scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<User>().Single();
        Assert.NotEqual(TestApi.DefaultPassword, stored.PasswordHash.Value);
        Assert.StartsWith("$2", stored.PasswordHash.Value);
    }

    [Fact]
    public async Task SignUp_IgnoresTheRoleSentByTheClient()
    {
        var response = await _client.PostAsJsonAsync(SignUpUrl, new
        {
            username = "mallory", email = "mallory@upc.edu.pe", password = TestApi.DefaultPassword,
            role = "Coordinator"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = (await response.Content.ReadFromJsonAsync<UserResource>())!;
        Assert.Equal("Student", user.Role);
    }

    [Fact]
    public async Task SignUp_WithTakenUsername_Returns409EvenWithDifferentCase()
    {
        await SignUp("Ana", "ana@upc.edu.pe");

        var response = await SignUp("ANA", "other@upc.edu.pe");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("UsernameAlreadyTaken", (await ReadProblem(response)).Title);
    }

    [Fact]
    public async Task SignUp_WithTakenEmail_Returns409()
    {
        await SignUp("ana", "ana@upc.edu.pe");

        var response = await SignUp("other", "ANA@upc.edu.pe");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("EmailAlreadyTaken", (await ReadProblem(response)).Title);
    }

    [Theory]
    [InlineData("ana", "ana@gmail.com", "password123", "InvalidInstitutionalEmail")]
    [InlineData("ana", "ana@upc.edu", "password123", "InvalidInstitutionalEmail")]
    [InlineData("ana", "ana@edu.pe", "password123", "InvalidInstitutionalEmail")]
    [InlineData("ab", "ana@upc.edu.pe", "password123", "InvalidUsername")]
    [InlineData("with space", "ana@upc.edu.pe", "password123", "InvalidUsername")]
    [InlineData("ana", "ana@upc.edu.pe", "short", "WeakPassword")]
    public async Task SignUp_WithInvalidData_Returns400WithTheErrorCode(string username, string email,
        string password, string expectedError)
    {
        var response = await SignUp(username, email, password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(expectedError, (await ReadProblem(response)).Title);
    }

    // ---------- Sign in ----------

    [Fact]
    public async Task SignIn_WithCorrectCredentials_Returns200AndAWorkingToken()
    {
        await SignUp("ana", "ana@upc.edu.pe");

        var response = await SignIn("ANA");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authenticated = (await response.Content.ReadFromJsonAsync<AuthenticatedUserResource>())!;
        Assert.False(string.IsNullOrWhiteSpace(authenticated.Token));

        using var authorized = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        authorized.Headers.Authorization = new("Bearer", authenticated.Token);
        var me = await _client.SendAsync(authorized);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal("ana", (await me.Content.ReadFromJsonAsync<UserResource>())!.Username);
    }

    [Fact]
    public async Task SignIn_WithWrongPasswordOrUnknownUser_Returns401WithTheSameError()
    {
        await SignUp("ana", "ana@upc.edu.pe");

        var wrongPassword = await SignIn("ana", "wrong-password");
        var unknownUser = await SignIn("nobody");

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        var first = await ReadProblem(wrongPassword);
        var second = await ReadProblem(unknownUser);
        Assert.Equal("InvalidCredentials", first.Title);
        Assert.Equal(first.Title, second.Title);
        Assert.Equal(first.Detail, second.Detail);
    }

    // ---------- Localization ----------

    [Theory]
    [InlineData("es-PE", "Usuario o contraseña incorrectos.")]
    [InlineData("es-MX", "Usuario o contraseña incorrectos.")]
    [InlineData("es", "Usuario o contraseña incorrectos.")]
    [InlineData("en-US", "Invalid username or password.")]
    [InlineData("fr-FR", "Invalid username or password.")]
    [InlineData(null, "Invalid username or password.")]
    public async Task ErrorMessages_FollowTheAcceptLanguageHeader(string? language, string expectedDetail)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, SignInUrl)
        {
            Content = JsonContent.Create(new SignInResource("nobody", TestApi.DefaultPassword))
        };
        if (language is not null) request.Headers.AcceptLanguage.ParseAdd(language);

        var response = await _client.SendAsync(request);

        Assert.Equal(expectedDetail, (await ReadProblem(response)).Detail);
    }
}