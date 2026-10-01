using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SkillSwap.Platform.CredentialVerification.Interfaces.Rest.Resources;
using SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Integration;

public class LearningPathEndpointsTests : ApiTestBase
{
    private const string PathsUrl = "/api/v1/learning-paths";
    private const string RestAndJwt = "quiero aprender a construir APIs REST con autenticación JWT";

    private static Task<HttpResponseMessage> DeclareAsync(HttpClient client, string goal)
    {
        return client.PostAsJsonAsync(PathsUrl, new DeclareGoalResource(goal));
    }

    private static async Task<LearningPathResource> DeclareOkAsync(SignedInUser user, string goal = RestAndJwt)
    {
        var response = await DeclareAsync(user.Client, goal);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LearningPathResource>())!;
    }

    private static async Task<string?> ErrorTitle(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Title;
    }

    // ---------- Declare goal ----------

    [Fact]
    public async Task Declare_WithAnInterpretableGoal_Returns201WithTheOrderedNodes()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await DeclareAsync(ana.Client, RestAndJwt);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var path = (await response.Content.ReadFromJsonAsync<LearningPathResource>())!;
        Assert.Equal(ana.Id, path.StudentId);
        Assert.Equal(RestAndJwt, path.Goal);
        Assert.Equal("Active", path.Status);
        Assert.Equal(["authentication-jwt", "rest-api-design"], path.GoalSkillTags.Order());
        Assert.Equal(
            ["networking-basics", "programming-fundamentals", "http-basics", "rest-api-design", "authentication-jwt"],
            path.Nodes.Select(n => n.SkillTag));
        Assert.Equal(["Available", "Available", "Locked", "Locked", "Locked"], path.Nodes.Select(n => n.Status));
        Assert.Equal("REST API design", path.Nodes.Single(n => n.SkillTag == "rest-api-design").SkillName);
        Assert.Equal(["http-basics", "programming-fundamentals"],
            path.Nodes.Single(n => n.SkillTag == "rest-api-design").PrerequisiteSkillTags);
        Assert.Equal($"{PathsUrl}/{ana.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Declare_IgnoresAStudentIdSentInTheBody()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await ana.Client.PostAsJsonAsync(PathsUrl, new { goal = RestAndJwt, studentId = 999 });

        var path = (await response.Content.ReadFromJsonAsync<LearningPathResource>())!;
        Assert.Equal(ana.Id, path.StudentId);
    }

    [Fact]
    public async Task Declare_WithAGoalThatMatchesNoSkill_Returns422()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await DeclareAsync(ana.Client, "quiero cocinar pasteles");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("GoalNotInterpretable", await ErrorTitle(response));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Declare_WithABlankGoal_Returns400(string goal)
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await DeclareAsync(ana.Client, goal);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidGoal", await ErrorTitle(response));
    }

    [Fact]
    public async Task Declare_WithoutAGoalProperty_Returns400()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await ana.Client.PostAsJsonAsync(PathsUrl, new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidGoal", await ErrorTitle(response));
    }

    [Fact]
    public async Task Declare_WithAGoalOver500Characters_Returns400()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await DeclareAsync(ana.Client, new string('a', 501));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Declare_WhenThereIsAnActivePath_Returns409()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        await DeclareOkAsync(ana);

        var response = await DeclareAsync(ana.Client, "quiero aprender SQL");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("ActivePathAlreadyExists", await ErrorTitle(response));
    }

    [Fact]
    public async Task Declare_WithoutToken_Returns401()
    {
        var response = await DeclareAsync(TestApi.CreateClient(), RestAndJwt);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Declare_AsCoordinator_Returns403()
    {
        var coordinator = await TestApi.RegisterCoordinatorAsync("coord");

        var response = await DeclareAsync(coordinator.Client, RestAndJwt);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Declare_ErrorMessagesFollowTheAcceptLanguageHeader()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        using var request = new HttpRequestMessage(HttpMethod.Post, PathsUrl)
            { Content = JsonContent.Create(new DeclareGoalResource("quiero cocinar pasteles")) };
        request.Headers.AcceptLanguage.ParseAdd("es-PE");

        var response = await ana.Client.SendAsync(request);

        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
        Assert.StartsWith("No pudimos relacionar tu meta", problem.Detail);
    }

    // ---------- Get path ----------

    [Fact]
    public async Task Get_AsTheOwner_Returns200WithTheNodesAndTheirStates()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var declared = await DeclareOkAsync(ana, "quiero aprender HTTP");

        var response = await ana.Client.GetAsync($"{PathsUrl}/{ana.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var path = (await response.Content.ReadFromJsonAsync<LearningPathResource>())!;
        Assert.Equal(declared.Id, path.Id);
        Assert.Equal(["networking-basics", "http-basics"], path.Nodes.Select(n => n.SkillTag));
        Assert.Equal(["Available", "Locked"], path.Nodes.Select(n => n.Status));
    }

    [Fact]
    public async Task Get_ForAStudentWithoutPath_Returns404()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await ana.Client.GetAsync($"{PathsUrl}/{ana.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("PathNotFound", await ErrorTitle(response));
    }

    [Fact]
    public async Task Get_AsAnotherStudent_Returns403()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var bob = await TestApi.RegisterStudentAsync("bob");
        await DeclareOkAsync(ana);

        var response = await bob.Client.GetAsync($"{PathsUrl}/{ana.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NotPathOwner", await ErrorTitle(response));
    }

    [Fact]
    public async Task Get_AsCoordinator_Returns200()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var coordinator = await TestApi.RegisterCoordinatorAsync("coord");
        await DeclareOkAsync(ana);

        var response = await coordinator.Client.GetAsync($"{PathsUrl}/{ana.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_AsCoordinatorForAStudentWithoutPath_Returns404()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var coordinator = await TestApi.RegisterCoordinatorAsync("coord");

        var response = await coordinator.Client.GetAsync($"{PathsUrl}/{ana.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        var response = await TestApi.CreateClient().GetAsync($"{PathsUrl}/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Certificates uploaded after declaring the goal ----------

    [Fact]
    public async Task Get_AsTheOwner_LinksCertificatesUploadedAfterTheGoal_AndACoordinatorReadNeverWrites()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var coordinator = await TestApi.RegisterCoordinatorAsync("coord");
        await DeclareOkAsync(ana);

        using var form = TestFiles.Form(TestFiles.Jpeg("cert"), "image/jpeg", [("courseName", "REST API fundamentals")]);
        var upload = await ana.Client.PostAsync("/api/v1/certificates", form);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var certificateId = (await upload.Content.ReadFromJsonAsync<CertificateResource>())!.Id;

        var byCoordinator = (await coordinator.Client.GetFromJsonAsync<LearningPathResource>($"{PathsUrl}/{ana.Id}"))!;
        Assert.All(byCoordinator.Nodes, n => Assert.Null(n.LinkedCertificateId));

        var byOwner = (await ana.Client.GetFromJsonAsync<LearningPathResource>($"{PathsUrl}/{ana.Id}"))!;
        var linked = Assert.Single(byOwner.Nodes, n => n.LinkedCertificateId is not null);
        Assert.Equal("rest-api-design", linked.SkillTag);
        Assert.Equal(certificateId, linked.LinkedCertificateId);
        Assert.Equal("Locked", linked.Status);

        // The link was saved: a later read by the coordinator sees it too.
        var again = (await coordinator.Client.GetFromJsonAsync<LearningPathResource>($"{PathsUrl}/{ana.Id}"))!;
        Assert.Equal(certificateId, again.Nodes.Single(n => n.SkillTag == "rest-api-design").LinkedCertificateId);
    }

    [Fact]
    public async Task Declare_LinksTheCertificatesTheStudentAlreadyHad()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        using var form = TestFiles.Form(TestFiles.Jpeg("cert"), "image/jpeg", [("courseName", "REST API fundamentals")]);
        var upload = await ana.Client.PostAsync("/api/v1/certificates", form);
        var certificateId = (await upload.Content.ReadFromJsonAsync<CertificateResource>())!.Id;

        var path = await DeclareOkAsync(ana);

        Assert.Equal(certificateId, path.Nodes.Single(n => n.SkillTag == "rest-api-design").LinkedCertificateId);
    }
}