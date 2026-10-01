using System.Net;
using Reqnroll;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;
using SkillSwap.Platform.Tests.Iam.Steps;
using SkillSwap.Platform.Tests.Support;
using System.Net.Http.Json;
using SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Steps;

[Binding]
public class AssessmentPeerReviewSteps(ApiScenarioContext context)
{
    // Reqnroll creates one instance of this class per scenario, so these fields live as long as it does.
    // Blueprints are kept per owner and skill, oldest first, so a scenario can answer an earlier one.
    private readonly Dictionary<string, List<AssessmentBlueprintResource>> _blueprints =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, int> _attemptIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _caseIds = new(StringComparer.OrdinalIgnoreCase);

    private static string Key(string username, string skillTag)
    {
        return $"{username}|{skillTag}";
    }

    private static int[] ParseAnswers(string text)
    {
        return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse).ToArray();
    }

    private SignedInUser User(string username)
    {
        return context.Users[username];
    }

    private async Task RequestBlueprintAsync(string username, string skillTag)
    {
        var path = await AssessmentFlow.GetPathAsync(User(username));
        var blueprint = await AssessmentFlow.RequestBlueprintAsync(User(username),
            AssessmentFlow.NodeId(path, skillTag));

        var key = Key(username, skillTag);
        if (!_blueprints.TryGetValue(key, out var history)) _blueprints[key] = history = [];
        history.Add(blueprint);
    }

    /// <summary>
    ///     Submits answers for one of the owner's assessments and remembers the attempt and the case it opened.
    /// </summary>
    private async Task SubmitAsync(string actor, string owner, string skillTag, IReadOnlyList<int>? answers,
        int correctCount, bool previous)
    {
        var history = _blueprints[Key(owner, skillTag)];
        var blueprint = previous ? history[^2] : history[^1];

        context.Response = await AssessmentFlow.SubmitAsync(User(actor), blueprint.Id,
            answers ?? AssessmentFlow.Answers(blueprint, correctCount));

        if (context.Response.StatusCode != HttpStatusCode.Created) return;

        var attempt = await context.ReadBodyAsync<AssessmentAttemptResource>();
        _attemptIds[actor] = attempt.Id;
        if (attempt.VerificationCaseId is { } caseId) _caseIds[actor] = caseId;
    }

    private async Task<VerificationCaseDetailResource> ReadCaseAsync(string actor, string owner)
    {
        var response = await AssessmentFlow.GetCaseAsync(User(actor), _caseIds[owner]);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await AssessmentFlow.ReadAsync<VerificationCaseDetailResource>(response);
    }

    private async Task ResolveAsync(string verifier, string owner, string decision, string notes)
    {
        context.Response = await AssessmentFlow.ResolveAsync(User(verifier), _caseIds[owner], decision, notes);
    }

    // ---------- Given ----------

    [Given("{string} has an assessment of the skill {string}")]
    public async Task GivenHasAnAssessmentOfTheSkill(string username, string skillTag)
    {
        await RequestBlueprintAsync(username, skillTag);
    }

    [Given("{string} has completed the skill {string}")]
    public async Task GivenHasCompletedTheSkill(string username, string skillTag)
    {
        await RequestBlueprintAsync(username, skillTag);
        await SubmitAsync(username, username, skillTag, null, 5, false);

        Assert.Equal(HttpStatusCode.Created, context.Response!.StatusCode);
        Assert.True((await context.ReadBodyAsync<AssessmentAttemptResource>()).Passed);
    }

    [Given("{string} has failed the assessment of the skill {string}")]
    [When("{string} has failed the assessment of the skill {string}")]
    public async Task GivenHasFailedTheAssessmentOfTheSkill(string username, string skillTag)
    {
        await RequestBlueprintAsync(username, skillTag);
        await SubmitAsync(username, username, skillTag, null, 0, false);

        Assert.Equal(HttpStatusCode.Created, context.Response!.StatusCode);
        var attempt = await context.ReadBodyAsync<AssessmentAttemptResource>();
        Assert.False(attempt.Passed);
        Assert.NotNull(attempt.VerificationCaseId);
    }

    [Given("{string} is a verifier of the skill {string}")]
    public async Task GivenIsAVerifierOfTheSkill(string username, string skillTag)
    {
        await AssessmentFlow.BecomeVerifierAsync(User(username), skillTag);
    }

    [Given("{string} switches their availability off")]
    public async Task GivenSwitchesTheirAvailabilityOff(string username)
    {
        context.Response = await AssessmentFlow.SetAvailabilityAsync(User(username), false);
        Assert.Equal(HttpStatusCode.OK, context.Response.StatusCode);
    }

    [Given("{string} has resolved the case of {string} as {string} with the notes {string}")]
    public async Task GivenHasResolvedTheCaseOf(string verifier, string owner, string decision, string notes)
    {
        await ResolveAsync(verifier, owner, decision, notes);
        Assert.Equal(HttpStatusCode.OK, context.Response!.StatusCode);
    }

    // ---------- When ----------

    [When("{string} submits {int} correct answers for the assessment of the skill {string}")]
    public async Task WhenSubmitsCorrectAnswers(string username, int correct, string skillTag)
    {
        await SubmitAsync(username, username, skillTag, null, correct, false);
    }

    [When("{string} submits {int} correct answers for the previous assessment of the skill {string}")]
    public async Task WhenSubmitsCorrectAnswersForThePreviousAssessment(string username, int correct,
        string skillTag)
    {
        await SubmitAsync(username, username, skillTag, null, correct, true);
    }

    [When("{string} submits {int} correct answers for the assessment of the skill {string} from the path of {string}")]
    public async Task WhenSubmitsCorrectAnswersFromThePathOf(string actor, int correct, string skillTag,
        string owner)
    {
        await SubmitAsync(actor, owner, skillTag, null, correct, false);
    }

    [When("{string} submits the answers {string} for the assessment of the skill {string}")]
    public async Task WhenSubmitsTheAnswers(string username, string answers, string skillTag)
    {
        await SubmitAsync(username, username, skillTag, ParseAnswers(answers), 0, false);
    }

    [When("{string} consults the last attempt of {string}")]
    public async Task WhenConsultsTheLastAttemptOf(string actor, string owner)
    {
        context.Response = await User(actor).Client.GetAsync($"{AssessmentFlow.AttemptsUrl}/{_attemptIds[owner]}");
    }

    [When("{string} creates a verifier profile for the skill {string}")]
    public async Task WhenCreatesAVerifierProfile(string username, string skillTag)
    {
        context.Response = await User(username).Client.PostAsJsonAsync(AssessmentFlow.ProfilesUrl,
            new CreateVerifierProfileResource(skillTag));
    }

    [When("{string} switches their availability off")]
    public async Task WhenSwitchesTheirAvailabilityOff(string username)
    {
        context.Response = await AssessmentFlow.SetAvailabilityAsync(User(username), false);
    }

    [When("{string} switches their availability on")]
    public async Task WhenSwitchesTheirAvailabilityOn(string username)
    {
        context.Response = await AssessmentFlow.SetAvailabilityAsync(User(username), true);
    }

    [When("{string} consults the cases assigned to them")]
    public async Task WhenConsultsTheCasesAssignedToThem(string username)
    {
        context.Response = await User(username).Client.GetAsync(AssessmentFlow.CasesUrl);
    }

    [When("{string} consults the case of {string}")]
    public async Task WhenConsultsTheCaseOf(string actor, string owner)
    {
        context.Response = await AssessmentFlow.GetCaseAsync(User(actor), _caseIds[owner]);
    }

    [When("{string} attaches the evidence {string} to the case of {string}")]
    public async Task WhenAttachesTheEvidence(string actor, string url, string owner)
    {
        context.Response = await AssessmentFlow.AttachEvidenceAsync(User(actor), _caseIds[owner], url);
    }

    [When("{string} resolves the case of {string} as {string} with the notes {string}")]
    public async Task WhenResolvesTheCase(string verifier, string owner, string decision, string notes)
    {
        await ResolveAsync(verifier, owner, decision, notes);
    }

    // ---------- Then ----------

    [Then("the attempt is approved with a score of {int} out of {int}")]
    public async Task ThenTheAttemptIsApproved(int score, int total)
    {
        var attempt = await context.ReadBodyAsync<AssessmentAttemptResource>();
        Assert.True(attempt.Passed);
        Assert.Equal(score, attempt.Score);
        Assert.Equal(total, attempt.TotalQuestions);
    }

    [Then("the attempt is not approved with a score of {int} out of {int}")]
    public async Task ThenTheAttemptIsNotApproved(int score, int total)
    {
        var attempt = await context.ReadBodyAsync<AssessmentAttemptResource>();
        Assert.False(attempt.Passed);
        Assert.Equal(score, attempt.Score);
        Assert.Equal(total, attempt.TotalQuestions);
    }

    [Then("a verification case is opened for the attempt")]
    public async Task ThenAVerificationCaseIsOpened()
    {
        var attempt = await context.ReadBodyAsync<AssessmentAttemptResource>();
        Assert.NotNull(attempt.VerificationCaseId);
    }

    [Then("the skill {string} of {string} is {string}")]
    public async Task ThenTheSkillOfIs(string skillTag, string username, string status)
    {
        var path = await AssessmentFlow.GetPathAsync(User(username));
        Assert.Equal(status, AssessmentFlow.NodeStatus(path, skillTag));
    }

    [Then("the verifier profile has the skills {string}")]
    public async Task ThenTheVerifierProfileHasTheSkills(string skills)
    {
        var profile = await context.ReadBodyAsync<VerifierProfileResource>();
        Assert.Equal(skills.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            profile.SkillTags);
    }

    [Then("the verifier profile is available")]
    public async Task ThenTheVerifierProfileIsAvailable()
    {
        Assert.True((await context.ReadBodyAsync<VerifierProfileResource>()).Available);
    }

    [Then("the verifier profile is not available")]
    public async Task ThenTheVerifierProfileIsNotAvailable()
    {
        Assert.False((await context.ReadBodyAsync<VerifierProfileResource>()).Available);
    }

    [Then("the number of assigned cases is {int}")]
    public async Task ThenTheNumberOfAssignedCasesIs(int count)
    {
        Assert.Equal(count, (await context.ReadBodyAsync<List<VerificationCaseResource>>()).Count);
    }

    [Then("the case of {string} is {string}")]
    public async Task ThenTheCaseOfIs(string owner, string status)
    {
        var detail = await ReadCaseAsync(owner, owner);
        Assert.Equal(status, detail.Case.Status);
    }

    [Then("the case of {string} is {string} to {string}")]
    public async Task ThenTheCaseOfIsAssignedTo(string owner, string status, string verifier)
    {
        var detail = await ReadCaseAsync(owner, owner);
        Assert.Equal(status, detail.Case.Status);
        Assert.Equal(User(verifier).Id, detail.Case.VerifierUserId);
    }

    [Then("the case lists {int} failed questions")]
    public async Task ThenTheCaseListsFailedQuestions(int count)
    {
        var detail = await context.ReadBodyAsync<VerificationCaseDetailResource>();
        Assert.Equal(count, detail.FailedQuestions.Count);
        Assert.All(detail.FailedQuestions, q => Assert.Equal(4, q.Answers.Count));
    }

    [Then("the case does not reveal the correct answers")]
    public async Task ThenTheCaseDoesNotRevealTheCorrectAnswers()
    {
        var body = await context.Response!.Content.ReadAsStringAsync();
        Assert.DoesNotContain("correct", body, StringComparison.OrdinalIgnoreCase);
    }

    [Then("the case is resolved as {string} with the notes {string}")]
    public async Task ThenTheCaseIsResolved(string decision, string notes)
    {
        var resolved = await context.ReadBodyAsync<VerificationCaseResource>();
        Assert.Equal("Resolved", resolved.Status);
        Assert.Equal(decision, resolved.Decision);
        Assert.Equal(notes, resolved.RubricNotes);
    }

    [Then("the case has the evidence {string}")]
    public async Task ThenTheCaseHasTheEvidence(string url)
    {
        Assert.Equal(url, (await context.ReadBodyAsync<VerificationCaseResource>()).EvidenceUrl);
    }
}