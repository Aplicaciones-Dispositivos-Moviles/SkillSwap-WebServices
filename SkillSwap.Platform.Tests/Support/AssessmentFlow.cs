using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;
using SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.Tests.Support;

/// <summary>
///     Drives the learning path, assessment and verification flows through the real API, for the
///     integration and BDD tests of Assessment &amp; Peer Review.
/// </summary>
public static class AssessmentFlow
{
    public const string RestAndJwt = "quiero aprender a construir APIs REST con autenticación JWT";
    public const string AttemptsUrl = "/api/v1/assessment-attempts";
    public const string CasesUrl = "/api/v1/verification-cases";
    public const string ProfilesUrl = "/api/v1/verifier-profiles";

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public static async Task<string?> ErrorOf(HttpResponseMessage response)
    {
        return (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Title;
    }

    // ---------- Learning path ----------

    public static async Task<LearningPathResource> DeclareGoalAsync(SignedInUser user, string goal = RestAndJwt)
    {
        var response = await user.Client.PostAsJsonAsync("/api/v1/learning-paths", new DeclareGoalResource(goal));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync<LearningPathResource>(response);
    }

    public static async Task<LearningPathResource> GetPathAsync(SignedInUser owner)
    {
        var response = await owner.Client.GetAsync($"/api/v1/learning-paths/{owner.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadAsync<LearningPathResource>(response);
    }

    public static int NodeId(LearningPathResource path, string skillTag)
    {
        return path.Nodes.Single(n => n.SkillTag == skillTag).Id;
    }

    public static string NodeStatus(LearningPathResource path, string skillTag)
    {
        return path.Nodes.Single(n => n.SkillTag == skillTag).Status;
    }

    // ---------- Assessments ----------

    public static async Task<AssessmentBlueprintResource> RequestBlueprintAsync(SignedInUser user, int nodeId)
    {
        var response = await user.Client.PostAsync($"/api/v1/path-nodes/{nodeId}/assessment-blueprint", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync<AssessmentBlueprintResource>(response);
    }

    /// <summary>
    ///     The answers that give exactly <paramref name="correctCount" /> hits. The fake question generator writes
    ///     "Question N?" and makes N % 4 the correct option, so the correct answers can be derived here even though
    ///     the API never reveals them.
    /// </summary>
    public static int[] Answers(AssessmentBlueprintResource blueprint, int correctCount)
    {
        return blueprint.Questions.Select((question, position) =>
        {
            var index = int.Parse(question.Question["Question ".Length..^1]);
            var correct = LearningPathTestData.Question(index).CorrectAnswer;
            return position < correctCount ? correct : (correct + 1) % 4;
        }).ToArray();
    }

    public static Task<HttpResponseMessage> SubmitAsync(SignedInUser user, int blueprintId,
        IReadOnlyList<int>? answers)
    {
        return user.Client.PostAsJsonAsync(AttemptsUrl, new SubmitAssessmentAttemptResource(blueprintId, answers));
    }

    public static async Task<AssessmentAttemptResource> SubmitOkAsync(SignedInUser user,
        AssessmentBlueprintResource blueprint, int correctCount)
    {
        var response = await SubmitAsync(user, blueprint.Id, Answers(blueprint, correctCount));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync<AssessmentAttemptResource>(response);
    }

    /// <summary>
    ///     The student requests the assessment of the skill and answers everything correctly.
    /// </summary>
    public static async Task<AssessmentAttemptResource> CompleteSkillAsync(SignedInUser user, string skillTag)
    {
        var path = await GetPathAsync(user);
        var blueprint = await RequestBlueprintAsync(user, NodeId(path, skillTag));
        var attempt = await SubmitOkAsync(user, blueprint, 5);
        Assert.True(attempt.Passed);
        return attempt;
    }

    /// <summary>
    ///     The student requests the assessment of the skill and answers every question wrong, which opens a case.
    /// </summary>
    public static async Task<(AssessmentBlueprintResource Blueprint, AssessmentAttemptResource Attempt)>
        FailSkillAsync(SignedInUser user, string skillTag)
    {
        var path = await GetPathAsync(user);
        var blueprint = await RequestBlueprintAsync(user, NodeId(path, skillTag));
        var attempt = await SubmitOkAsync(user, blueprint, 0);
        Assert.False(attempt.Passed);
        Assert.NotNull(attempt.VerificationCaseId);
        return (blueprint, attempt);
    }

    // ---------- Verifiers and cases ----------

    public static async Task<VerifierProfileResource> BecomeVerifierAsync(SignedInUser user, string skillTag)
    {
        var response = await user.Client.PostAsJsonAsync(ProfilesUrl, new CreateVerifierProfileResource(skillTag));
        Assert.True(response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK);
        return await ReadAsync<VerifierProfileResource>(response);
    }

    /// <summary>
    ///     Registers a student who completes the skill and becomes a verifier of it.
    /// </summary>
    public static async Task<SignedInUser> RegisterVerifierAsync(string username,
        string skillTag = "networking-basics")
    {
        var user = await TestApi.RegisterStudentAsync(username);
        await DeclareGoalAsync(user);
        await CompleteSkillAsync(user, skillTag);
        await BecomeVerifierAsync(user, skillTag);
        return user;
    }

    public static Task<HttpResponseMessage> SetAvailabilityAsync(SignedInUser user, bool available)
    {
        return PatchJsonAsync(user.Client, $"{ProfilesUrl}/me/availability", new VerifierAvailabilityResource(available));
    }

    public static Task<HttpResponseMessage> GetCaseAsync(SignedInUser user, int caseId)
    {
        return user.Client.GetAsync($"{CasesUrl}/{caseId}");
    }

    public static Task<HttpResponseMessage> AttachEvidenceAsync(SignedInUser user, int caseId, string? url)
    {
        return user.Client.PutAsJsonAsync($"{CasesUrl}/{caseId}/evidence", new AttachEvidenceResource(url));
    }

    public static Task<HttpResponseMessage> ResolveAsync(SignedInUser user, int caseId, string? decision,
        string? notes)
    {
        return PatchJsonAsync(user.Client, $"{CasesUrl}/{caseId}/decision", new ResolveCaseResource(decision, notes));
    }

    public static Task<HttpResponseMessage> PatchJsonAsync(HttpClient client, string url, object body)
    {
        return client.PatchAsync(url, JsonContent.Create(body, body.GetType()));
    }

    public static Task<HttpResponseMessage> PatchRawJsonAsync(HttpClient client, string url, string json)
    {
        return client.PatchAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
    }
    
    private static int _learnerCount;

    /// <summary>
    ///     The verifier resolves that many cases, each one failed by a new student, approving and rejecting
    ///     alternately. They earn 10 SkillCredits per case, whatever the decision.
    /// </summary>
    public static async Task EarnCreditsAsync(SignedInUser verifier, int cases)
    {
        for (var i = 0; i < cases; i++)
        {
            var student = await TestApi.RegisterStudentAsync($"learner{Interlocked.Increment(ref _learnerCount)}");
            await DeclareGoalAsync(student);
            var (_, attempt) = await FailSkillAsync(student, "networking-basics");

            var response = await ResolveAsync(verifier, attempt.VerificationCaseId!.Value,
                i % 2 == 0 ? "Approved" : "Rejected", "Reviewed following the rubric.");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}