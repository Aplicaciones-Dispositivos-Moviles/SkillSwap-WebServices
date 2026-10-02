using Reqnroll;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;
using SkillSwap.Platform.Reputation.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Iam.Steps;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Reputation.Steps;

/// <summary>
///     Steps to consult the reputation. Everything that produces it (passing an assessment, failing one,
///     becoming a verifier, resolving a case) is done with the steps of Assessment &amp; Peer Review.
/// </summary>
[Binding]
public class ReputationSteps(ApiScenarioContext context)
{
    private const string EmployabilityUrl = "/api/v1/student-employability-scores";
    private const string ReliabilityUrl = "/api/v1/verifier-reliabilities";

    // ---------- When ----------

    [When("{string} consults the employability of {string}")]
    public async Task WhenConsultsTheEmployabilityOf(string actor, string owner)
    {
        context.Response = await context.Users[actor].Client
            .GetAsync($"{EmployabilityUrl}/{context.Users[owner].Id}");
    }

    [When("{string} consults the reliability of {string}")]
    public async Task WhenConsultsTheReliabilityOf(string actor, string owner)
    {
        context.Response = await context.Users[actor].Client
            .GetAsync($"{ReliabilityUrl}/{context.Users[owner].Id}");
    }

    [When("{string} consults their verifier profile")]
    public async Task WhenConsultsTheirVerifierProfile(string username)
    {
        context.Response = await context.Users[username].Client.GetAsync($"{AssessmentFlow.ProfilesUrl}/me");
    }

    // ---------- Then ----------

    [Then("the employability shows {int} verified skills and a score of {int}")]
    public async Task ThenTheEmployabilityShows(int skills, int score)
    {
        var employability = await context.ReadBodyAsync<StudentEmployabilityResource>();
        Assert.Equal(skills, employability.VerifiedSkillsCount);
        Assert.Equal(score, employability.Score);
    }

    [Then("the reliability shows {int} resolved cases and a score of {int}")]
    public async Task ThenTheReliabilityShows(int cases, int score)
    {
        var reliability = await context.ReadBodyAsync<VerifierReliabilityResource>();
        Assert.Equal(cases, reliability.ResolvedCasesCount);
        Assert.Equal(score, reliability.Score);
    }

    [Then("the verifier profile has a rating of {int}")]
    public async Task ThenTheVerifierProfileHasARating(int rating)
    {
        var profile = await context.ReadBodyAsync<VerifierProfileResource>();
        Assert.Equal(rating, profile.Rating);
    }
}