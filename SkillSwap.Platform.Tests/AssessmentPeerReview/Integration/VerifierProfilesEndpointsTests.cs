using System.Net;
using System.Net.Http.Json;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Integration;

public class VerifierProfilesEndpointsTests : ApiTestBase
{
    private const string Skill = "networking-basics";

    private static async Task<SignedInUser> StudentWhoCompletedAsync(string username, params string[] skills)
    {
        var user = await TestApi.RegisterStudentAsync(username);
        await AssessmentFlow.DeclareGoalAsync(user);
        foreach (var skill in skills) await AssessmentFlow.CompleteSkillAsync(user, skill);
        return user;
    }

    private static Task<HttpResponseMessage> CreateAsync(SignedInUser user, string? skill)
    {
        return user.Client.PostAsJsonAsync(AssessmentFlow.ProfilesUrl, new CreateVerifierProfileResource(skill));
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_ForACompletedSkill_Returns201WithAnAvailableProfile()
    {
        var bob = await StudentWhoCompletedAsync("bob", Skill);

        var response = await CreateAsync(bob, Skill);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var profile = await AssessmentFlow.ReadAsync<VerifierProfileResource>(response);
        Assert.Equal(bob.Id, profile.VerifierUserId);
        Assert.Equal([Skill], profile.SkillTags);
        Assert.True(profile.Available);
        Assert.True(profile.Verified);
        Assert.Equal(0, profile.ReviewCount);
        Assert.EndsWith("/api/v1/verifier-profiles/me", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Create_ForASecondSkill_Returns200AndAddsItToTheProfile()
    {
        var bob = await StudentWhoCompletedAsync("bob", Skill, "http-basics");
        await CreateAsync(bob, Skill);

        var response = await CreateAsync(bob, "http-basics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await AssessmentFlow.ReadAsync<VerifierProfileResource>(response);
        Assert.Equal(["http-basics", Skill], profile.SkillTags);
    }

    [Fact]
    public async Task Create_ForASkillAlreadyEnabled_Returns409()
    {
        var bob = await StudentWhoCompletedAsync("bob", Skill);
        await CreateAsync(bob, Skill);

        var response = await CreateAsync(bob, Skill);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("VerifierSkillAlreadyEnabled", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Create_ForASkillThatWasNotCompleted_Returns409()
    {
        var bob = await StudentWhoCompletedAsync("bob");

        var response = await CreateAsync(bob, Skill);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("SkillNotCompleted", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Create_ForASkillCompletedByAnotherStudent_Returns409()
    {
        await StudentWhoCompletedAsync("bob", Skill);
        var ana = await StudentWhoCompletedAsync("ana");

        var response = await CreateAsync(ana, Skill);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("SkillNotCompleted", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Create_WithoutASkill_Returns400()
    {
        var bob = await StudentWhoCompletedAsync("bob", Skill);

        var response = await CreateAsync(bob, null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidSkillTag", await AssessmentFlow.ErrorOf(response));
    }

    [Fact]
    public async Task Create_AsACoordinator_Returns403()
    {
        var admin = await TestApi.RegisterCoordinatorAsync("admin");

        var response = await CreateAsync(admin, Skill);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutAToken_Returns401()
    {
        var response = await TestApi.CreateClient().PostAsJsonAsync(AssessmentFlow.ProfilesUrl,
            new CreateVerifierProfileResource(Skill));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Get mine ----------

    [Fact]
    public async Task GetMine_ForAVerifier_Returns200()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");

        var response = await bob.Client.GetAsync($"{AssessmentFlow.ProfilesUrl}/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await AssessmentFlow.ReadAsync<VerifierProfileResource>(response);
        Assert.Equal(bob.Id, profile.VerifierUserId);
        Assert.Equal([Skill], profile.SkillTags);
    }

    [Fact]
    public async Task GetMine_ForAStudentWhoIsNotAVerifier_Returns404()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await ana.Client.GetAsync($"{AssessmentFlow.ProfilesUrl}/me");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("VerifierProfileNotFound", await AssessmentFlow.ErrorOf(response));
    }

    // ---------- Availability ----------

    [Fact]
    public async Task Availability_SwitchedOffAndOn_ReturnsTheUpdatedProfile()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");

        var off = await AssessmentFlow.SetAvailabilityAsync(bob, false);
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        Assert.False((await AssessmentFlow.ReadAsync<VerifierProfileResource>(off)).Available);

        var on = await AssessmentFlow.SetAvailabilityAsync(bob, true);
        Assert.Equal(HttpStatusCode.OK, on.StatusCode);
        Assert.True((await AssessmentFlow.ReadAsync<VerifierProfileResource>(on)).Available);
    }

    [Fact]
    public async Task Availability_IsPersisted()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        await AssessmentFlow.SetAvailabilityAsync(bob, false);

        var response = await bob.Client.GetAsync($"{AssessmentFlow.ProfilesUrl}/me");

        Assert.False((await AssessmentFlow.ReadAsync<VerifierProfileResource>(response)).Available);
    }

    [Fact]
    public async Task Availability_WithoutTheValue_Returns400AndChangesNothing()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");

        var response = await AssessmentFlow.PatchRawJsonAsync(bob.Client,
            $"{AssessmentFlow.ProfilesUrl}/me/availability", "{}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidAvailability", await AssessmentFlow.ErrorOf(response));
        var profile = await AssessmentFlow.ReadAsync<VerifierProfileResource>(
            await bob.Client.GetAsync($"{AssessmentFlow.ProfilesUrl}/me"));
        Assert.True(profile.Available);
    }

    [Fact]
    public async Task Availability_ForAStudentWhoIsNotAVerifier_Returns403()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var response = await AssessmentFlow.SetAvailabilityAsync(ana, false);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NotAVerifier", await AssessmentFlow.ErrorOf(response));
    }
}