using SkillSwap.Platform.AssessmentPeerReview.Application.Internal.QueryServices;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Queries;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Application;

public class QueryServicesTests
{
    private readonly FakeAssessmentAttemptRepository _attempts = new();
    private readonly FakeVerificationCaseRepository _cases = new();
    private readonly FakeLearningPathContextFacade _learningPath = new();
    private readonly FakeVerifierProfileRepository _profiles = new();

    private VerificationCaseQueryService CaseService()
    {
        return new VerificationCaseQueryService(_cases, _attempts, _learningPath);
    }

    private async Task<AssessmentAttempt> AddAttemptAsync(int[] answers, int blueprintId = 1)
    {
        var attempt = new AssessmentAttempt(blueprintId, 1, answers, FakeLearningPathContextFacade.Correct);
        await _attempts.AddAsync(attempt);
        return attempt;
    }

    // ---------- Attempts and profiles ----------

    [Fact]
    public async Task AttemptQuery_ReturnsTheAttemptOrNull()
    {
        var attempt = await AddAttemptAsync([1, 2, 3, 0, 1]);
        var service = new AssessmentAttemptQueryService(_attempts);

        Assert.Same(attempt, await service.Handle(new GetAssessmentAttemptByIdQuery(attempt.Id), CancellationToken.None));
        Assert.Null(await service.Handle(new GetAssessmentAttemptByIdQuery(99), CancellationToken.None));
    }

    [Fact]
    public async Task ProfileQuery_ReturnsTheProfileOfTheUserOrNull()
    {
        var profile = new VerifierProfile(2, "http-basics");
        await _profiles.AddAsync(profile);
        var service = new VerifierProfileQueryService(_profiles);

        Assert.Same(profile, await service.Handle(new GetVerifierProfileByUserIdQuery(2), CancellationToken.None));
        Assert.Null(await service.Handle(new GetVerifierProfileByUserIdQuery(3), CancellationToken.None));
    }

    // ---------- Cases ----------

    [Fact]
    public async Task CaseQuery_ById_ReturnsTheCaseOrNull()
    {
        var verificationCase = new VerificationCase(1, 1, 10, "http-basics");
        await _cases.AddAsync(verificationCase);

        Assert.Same(verificationCase,
            await CaseService().Handle(new GetVerificationCaseByIdQuery(verificationCase.Id), CancellationToken.None));
        Assert.Null(await CaseService().Handle(new GetVerificationCaseByIdQuery(99), CancellationToken.None));
    }

    [Fact]
    public async Task CaseQuery_ByVerifier_ReturnsOnlyTheirCasesNewestFirst()
    {
        var first = new VerificationCase(1, 1, 10, "http-basics").AssignVerifier(2);
        var other = new VerificationCase(2, 1, 11, "http-basics").AssignVerifier(3);
        var second = new VerificationCase(3, 4, 12, "http-basics").AssignVerifier(2);
        await _cases.AddAsync(first);
        await _cases.AddAsync(other);
        await _cases.AddAsync(second);

        var found = await CaseService().Handle(new GetVerificationCasesByVerifierQuery(2), CancellationToken.None);

        Assert.Equal([second.Id, first.Id], found.Select(c => c.Id));
    }

    [Fact]
    public async Task CaseQuery_Detail_ListsTheFailedQuestionsWithTheChosenAnswer()
    {
        _learningPath.AddBlueprint();
        var attempt = await AddAttemptAsync([1, 0, 3, 0, 2]);
        var verificationCase = new VerificationCase(attempt.Id, 1, 10, "http-basics");
        await _cases.AddAsync(verificationCase);

        var detail = await CaseService().Handle(new GetVerificationCaseDetailQuery(verificationCase.Id),
            CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Same(verificationCase, detail.Case);
        Assert.Same(attempt, detail.Attempt);
        Assert.Equal([2, 5], detail.FailedQuestions.Select(q => q.Position));
        Assert.Equal(["Question 2?", "Question 5?"], detail.FailedQuestions.Select(q => q.Text));
        Assert.Equal([0, 2], detail.FailedQuestions.Select(q => q.SelectedAnswer));
        Assert.Equal(["a2", "b2", "c2", "d2"], detail.FailedQuestions[0].Answers);
    }

    [Fact]
    public async Task CaseQuery_Detail_WhenEverythingWasCorrect_HasNoFailedQuestions()
    {
        _learningPath.AddBlueprint();
        var attempt = await AddAttemptAsync([1, 2, 3, 0, 1]);
        var verificationCase = new VerificationCase(attempt.Id, 1, 10, "http-basics");
        await _cases.AddAsync(verificationCase);

        var detail = await CaseService().Handle(new GetVerificationCaseDetailQuery(verificationCase.Id),
            CancellationToken.None);

        Assert.Empty(detail!.FailedQuestions);
    }

    [Fact]
    public async Task CaseQuery_Detail_WhenTheBlueprintIsGone_HasNoFailedQuestions()
    {
        var attempt = await AddAttemptAsync([1, 0, 3, 0, 2]);
        var verificationCase = new VerificationCase(attempt.Id, 1, 10, "http-basics");
        await _cases.AddAsync(verificationCase);

        var detail = await CaseService().Handle(new GetVerificationCaseDetailQuery(verificationCase.Id),
            CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Empty(detail.FailedQuestions);
    }

    [Fact]
    public async Task CaseQuery_Detail_ForAnUnknownCase_ReturnsNull()
    {
        Assert.Null(await CaseService().Handle(new GetVerificationCaseDetailQuery(99), CancellationToken.None));
    }

    [Fact]
    public async Task CaseQuery_Detail_WhenTheAttemptIsMissing_ReturnsNull()
    {
        var verificationCase = new VerificationCase(50, 1, 10, "http-basics");
        await _cases.AddAsync(verificationCase);

        Assert.Null(await CaseService().Handle(new GetVerificationCaseDetailQuery(verificationCase.Id),
            CancellationToken.None));
    }
}