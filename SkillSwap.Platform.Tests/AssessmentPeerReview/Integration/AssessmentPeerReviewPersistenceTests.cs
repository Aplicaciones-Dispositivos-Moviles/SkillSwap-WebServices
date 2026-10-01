using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Integration;

public class AssessmentPeerReviewPersistenceTests : ApiTestBase
{
    private static readonly int[] Correct = [1, 2, 3, 0, 1];

    private static async Task SaveAsync<TRepository, TEntity>(TEntity entity)
        where TRepository : class, IBaseRepository<TEntity>
        where TEntity : class
    {
        using var scope = TestApi.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TRepository>().AddAsync(entity);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
    }

    private static async Task ChangeCaseAsync(int caseId, Action<VerificationCase> change)
    {
        using var scope = TestApi.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IVerificationCaseRepository>();
        var verificationCase = (await repository.FindByIdAsync(caseId))!;
        change(verificationCase);
        repository.Update(verificationCase);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
    }

    private static async Task ChangeProfileAsync(int userId, Action<VerifierProfile> change)
    {
        using var scope = TestApi.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IVerifierProfileRepository>();
        var profile = (await repository.FindByUserIdAsync(userId, default))!;
        change(profile);
        repository.Update(profile);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
    }

    private static async Task<VerificationCase> AddCaseAsync(int attemptId, int studentId = 1, int nodeId = 10,
        string skill = "http-basics")
    {
        var verificationCase = new VerificationCase(attemptId, studentId, nodeId, skill);
        await SaveAsync<IVerificationCaseRepository, VerificationCase>(verificationCase);
        return verificationCase;
    }

    private static async Task<VerificationCase> LoadCaseAsync(int caseId)
    {
        using var scope = TestApi.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<IVerificationCaseRepository>()
            .FindByIdAsync(caseId))!;
    }

    // ---------- Attempts ----------

    [Fact]
    public async Task Attempt_RoundTripsTheAnswersTheScoreAndTheResult()
    {
        await SaveAsync<IAssessmentAttemptRepository, AssessmentAttempt>(
            new AssessmentAttempt(5, 7, [1, 2, 3, 0, 3], Correct));

        using var scope = TestApi.CreateScope();
        var attempt = await scope.ServiceProvider.GetRequiredService<IAssessmentAttemptRepository>()
            .FindByBlueprintIdAsync(5, default);

        Assert.NotNull(attempt);
        Assert.True(attempt.Id > 0);
        Assert.Equal(7, attempt.StudentId);
        Assert.Equal([1, 2, 3, 0, 3], attempt.SelectedAnswers);
        Assert.Equal(new Score(4, 5), attempt.Score);
        Assert.True(attempt.Passed);
        Assert.Equal(DateTimeKind.Utc, attempt.CompletedAt.Kind);
    }

    [Fact]
    public async Task Attempt_FindByBlueprintId_WithAnUnknownBlueprint_ReturnsNull()
    {
        using var scope = TestApi.CreateScope();

        Assert.Null(await scope.ServiceProvider.GetRequiredService<IAssessmentAttemptRepository>()
            .FindByBlueprintIdAsync(99, default));
    }

    [Fact]
    public async Task Attempt_TwoForTheSameBlueprint_ViolateTheUniqueIndex()
    {
        await SaveAsync<IAssessmentAttemptRepository, AssessmentAttempt>(
            new AssessmentAttempt(5, 7, Correct, Correct));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            SaveAsync<IAssessmentAttemptRepository, AssessmentAttempt>(
                new AssessmentAttempt(5, 7, [0, 0, 0, 1, 0], Correct)));
    }

    // ---------- Verifier profiles ----------

    [Fact]
    public async Task Profile_RoundTripsTheSkillsTheFlagsAndTheCounters()
    {
        var profile = new VerifierProfile(3, "rest-api-design");
        profile.AddSkill("http-basics");
        profile.SetAvailability(false).IncrementReviewCount().UpdateRating(4.5);
        await SaveAsync<IVerifierProfileRepository, VerifierProfile>(profile);

        using var scope = TestApi.CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<IVerifierProfileRepository>()
            .FindByUserIdAsync(3, default);

        Assert.NotNull(loaded);
        Assert.True(loaded.Id > 0);
        Assert.Equal(["http-basics", "rest-api-design"], loaded.SkillTags);
        Assert.False(loaded.Available);
        Assert.True(loaded.Verified);
        Assert.Equal(4.5, loaded.Rating);
        Assert.Equal(1, loaded.ReviewCount);
        Assert.Equal(DateTimeKind.Utc, loaded.CreatedAt.Kind);
    }

    [Fact]
    public async Task Profile_AddedSkillsAndChangedFlags_ArePersisted()
    {
        await SaveAsync<IVerifierProfileRepository, VerifierProfile>(new VerifierProfile(3, "http-basics"));

        await ChangeProfileAsync(3, profile =>
        {
            profile.AddSkill("sql-fundamentals");
            profile.SetAvailability(false);
            profile.IncrementReviewCount();
        });

        using var scope = TestApi.CreateScope();
        var loaded = (await scope.ServiceProvider.GetRequiredService<IVerifierProfileRepository>()
            .FindByUserIdAsync(3, default))!;
        Assert.Equal(["http-basics", "sql-fundamentals"], loaded.SkillTags);
        Assert.False(loaded.Available);
        Assert.Equal(1, loaded.ReviewCount);
    }

    [Fact]
    public async Task Profile_FindByUserId_WithAnUnknownUser_ReturnsNull()
    {
        using var scope = TestApi.CreateScope();

        Assert.Null(await scope.ServiceProvider.GetRequiredService<IVerifierProfileRepository>()
            .FindByUserIdAsync(99, default));
    }

    [Fact]
    public async Task Profile_TwoForTheSameUser_ViolateTheUniqueIndex()
    {
        await SaveAsync<IVerifierProfileRepository, VerifierProfile>(new VerifierProfile(3, "http-basics"));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            SaveAsync<IVerifierProfileRepository, VerifierProfile>(new VerifierProfile(3, "sql-fundamentals")));
    }

    [Fact]
    public async Task Profile_FindEnabledBySkillTag_ReturnsTheProfilesNotRevokedWithThatSkill()
    {
        await SaveAsync<IVerifierProfileRepository, VerifierProfile>(new VerifierProfile(2, "http-basics"));
        await SaveAsync<IVerifierProfileRepository, VerifierProfile>(
            new VerifierProfile(3, "http-basics").SetAvailability(false));
        await SaveAsync<IVerifierProfileRepository, VerifierProfile>(new VerifierProfile(4, "http-basics").Revoke());
        await SaveAsync<IVerifierProfileRepository, VerifierProfile>(new VerifierProfile(5, "sql-fundamentals"));

        using var scope = TestApi.CreateScope();
        var found = await scope.ServiceProvider.GetRequiredService<IVerifierProfileRepository>()
            .FindEnabledBySkillTagAsync("http-basics", default);

        Assert.Equal([2, 3], found.Select(p => p.VerifierUserId).Order());
    }

    // ---------- Verification cases ----------

    [Fact]
    public async Task Case_RoundTripsEveryStageOfItsLifecycle()
    {
        var created = await AddCaseAsync(attemptId: 1, studentId: 1, nodeId: 10);

        var pending = await LoadCaseAsync(created.Id);
        Assert.Equal(CaseStatus.Pending, pending.Status);
        Assert.Equal("http-basics", pending.SkillTag);
        Assert.Equal(10, pending.PathNodeId);
        Assert.Null(pending.VerifierUserId);
        Assert.Null(pending.Decision);
        Assert.Null(pending.AssignedAt);
        Assert.Equal(DateTimeKind.Utc, pending.OpenedAt.Kind);

        await ChangeCaseAsync(created.Id, c => c.AssignVerifier(2).AttachEvidence("https://github.com/student/app"));
        var assigned = await LoadCaseAsync(created.Id);
        Assert.Equal(CaseStatus.Assigned, assigned.Status);
        Assert.Equal(2, assigned.VerifierUserId);
        Assert.Equal("https://github.com/student/app", assigned.EvidenceUrl);
        Assert.NotNull(assigned.AssignedAt);

        await ChangeCaseAsync(created.Id, c => c.Resolve(ReviewDecision.Approved, "Meets the rubric."));
        var resolved = await LoadCaseAsync(created.Id);
        Assert.Equal(CaseStatus.Resolved, resolved.Status);
        Assert.Equal(ReviewDecision.Approved, resolved.Decision);
        Assert.Equal("Meets the rubric.", resolved.RubricNotes);
        Assert.NotNull(resolved.ResolvedAt);
        Assert.Equal(DateTimeKind.Utc, resolved.ResolvedAt!.Value.Kind);
    }

    [Fact]
    public async Task Case_FindByVerifierUserId_ReturnsTheirCasesNewestFirst()
    {
        var first = await AddCaseAsync(1, studentId: 1, nodeId: 10);
        var other = await AddCaseAsync(2, studentId: 4, nodeId: 11);
        var second = await AddCaseAsync(3, studentId: 5, nodeId: 12);
        await ChangeCaseAsync(first.Id, c => c.AssignVerifier(2));
        await ChangeCaseAsync(other.Id, c => c.AssignVerifier(3));
        await ChangeCaseAsync(second.Id, c => c.AssignVerifier(2));

        using var scope = TestApi.CreateScope();
        var found = await scope.ServiceProvider.GetRequiredService<IVerificationCaseRepository>()
            .FindByVerifierUserIdAsync(2, default);

        Assert.Equal([second.Id, first.Id], found.Select(c => c.Id));
    }

    [Fact]
    public async Task Case_FindOpenByStudentAndNode_IgnoresResolvedCases()
    {
        var created = await AddCaseAsync(1, studentId: 1, nodeId: 10);

        using (var scope = TestApi.CreateScope())
        {
            var open = await scope.ServiceProvider.GetRequiredService<IVerificationCaseRepository>()
                .FindOpenByStudentAndNodeAsync(1, 10, default);
            Assert.Equal(created.Id, open!.Id);
        }

        await ChangeCaseAsync(created.Id, c => c.AssignVerifier(2).Resolve(ReviewDecision.Rejected, "Needs work."));

        using (var scope = TestApi.CreateScope())
        {
            Assert.Null(await scope.ServiceProvider.GetRequiredService<IVerificationCaseRepository>()
                .FindOpenByStudentAndNodeAsync(1, 10, default));
        }
    }

    [Fact]
    public async Task Case_TwoOpenCasesForTheSameStudentAndNode_ViolateTheUniqueIndex()
    {
        await AddCaseAsync(1, studentId: 1, nodeId: 10);

        await Assert.ThrowsAsync<DbUpdateException>(() => AddCaseAsync(2, studentId: 1, nodeId: 10));
    }

    [Fact]
    public async Task Case_ANewOneIsAllowedOnceThePreviousWasResolved()
    {
        var first = await AddCaseAsync(1, studentId: 1, nodeId: 10);
        await ChangeCaseAsync(first.Id, c => c.AssignVerifier(2).Resolve(ReviewDecision.Rejected, "Needs work."));

        var second = await AddCaseAsync(2, studentId: 1, nodeId: 10);

        Assert.True(second.Id > first.Id);
    }

    [Fact]
    public async Task Case_TwoForTheSameAttempt_ViolateTheUniqueIndex()
    {
        await AddCaseAsync(1, studentId: 1, nodeId: 10);

        await Assert.ThrowsAsync<DbUpdateException>(() => AddCaseAsync(1, studentId: 4, nodeId: 11));
    }

    [Fact]
    public async Task Case_FindPendingBySkillTag_ReturnsOnlyPendingCasesOfTheSkillOldestFirst()
    {
        var first = await AddCaseAsync(1, studentId: 1, nodeId: 10);
        var second = await AddCaseAsync(2, studentId: 4, nodeId: 11);
        await AddCaseAsync(3, studentId: 5, nodeId: 12, skill: "sql-fundamentals");
        var assigned = await AddCaseAsync(4, studentId: 6, nodeId: 13);
        await ChangeCaseAsync(assigned.Id, c => c.AssignVerifier(2));

        using var scope = TestApi.CreateScope();
        var found = await scope.ServiceProvider.GetRequiredService<IVerificationCaseRepository>()
            .FindPendingBySkillTagAsync("http-basics", default);

        Assert.Equal([first.Id, second.Id], found.Select(c => c.Id));
    }

    [Fact]
    public async Task Case_CountOpenByVerifierUserIds_CountsOnlyUnresolvedCasesAndDefaultsToZero()
    {
        var one = await AddCaseAsync(1, studentId: 1, nodeId: 10);
        var two = await AddCaseAsync(2, studentId: 4, nodeId: 11);
        var done = await AddCaseAsync(3, studentId: 5, nodeId: 12);
        await ChangeCaseAsync(one.Id, c => c.AssignVerifier(2));
        await ChangeCaseAsync(two.Id, c => c.AssignVerifier(2));
        await ChangeCaseAsync(done.Id, c => c.AssignVerifier(2).Resolve(ReviewDecision.Approved, "Good."));

        using var scope = TestApi.CreateScope();
        var counts = await scope.ServiceProvider.GetRequiredService<IVerificationCaseRepository>()
            .CountOpenByVerifierUserIdsAsync([2, 3], default);

        Assert.Equal(2, counts[2]);
        Assert.Equal(0, counts[3]);
    }
}