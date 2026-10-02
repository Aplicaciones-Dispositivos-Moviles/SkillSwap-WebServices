using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Repositories;
using SkillSwap.Platform.Reputation.Domain.Services;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Reputation.Integration;

public class ReputationPersistenceTests : ApiTestBase
{
    private static readonly VerifierReliabilityCalculator ReliabilityCalculator = new();
    private static readonly EmployabilityScoreCalculator EmployabilityCalculator = new();

    private static async Task SaveAsync<TRepository, TEntity>(TEntity entity)
        where TRepository : class, IBaseRepository<TEntity>
        where TEntity : class
    {
        using var scope = TestApi.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TRepository>().AddAsync(entity);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
    }

    private static async Task<VerifierReliability?> LoadReliabilityAsync(int verifierUserId)
    {
        using var scope = TestApi.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IVerifierReliabilityRepository>()
            .FindByVerifierUserIdAsync(verifierUserId, default);
    }

    private static async Task<StudentEmployabilityScore?> LoadEmployabilityAsync(int studentId)
    {
        using var scope = TestApi.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IStudentEmployabilityScoreRepository>()
            .FindByStudentIdAsync(studentId, default);
    }

    // ---------- Verifier reliability ----------

    [Fact]
    public async Task Reliability_RoundTripsTheCountersAndTheScore()
    {
        var reliability = new VerifierReliability(7)
            .RecordResolution(ReliabilityCalculator)
            .RecordResolution(ReliabilityCalculator)
            .RecordResolution(ReliabilityCalculator)
            .RecordOverturn(ReliabilityCalculator)
            .ApplySanction(ReliabilityCalculator);
        await SaveAsync<IVerifierReliabilityRepository, VerifierReliability>(reliability);

        var loaded = await LoadReliabilityAsync(7);

        Assert.NotNull(loaded);
        Assert.True(loaded.Id > 0);
        Assert.Equal(3, loaded.ResolvedCasesCount);
        Assert.Equal(1, loaded.OverturnedDecisionsCount);
        Assert.Equal(1, loaded.SanctionsCount);
        Assert.Equal(60, loaded.Score.Value);
        Assert.Equal(DateTimeKind.Utc, loaded.UpdatedAt.Kind);
    }

    [Fact]
    public async Task Reliability_FindByVerifierUserId_WithAnUnknownUser_ReturnsNull()
    {
        Assert.Null(await LoadReliabilityAsync(99));
    }

    [Fact]
    public async Task Reliability_Changes_ArePersisted()
    {
        await SaveAsync<IVerifierReliabilityRepository, VerifierReliability>(new VerifierReliability(7));

        using (var scope = TestApi.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IVerifierReliabilityRepository>();
            var reliability = (await repository.FindByVerifierUserIdAsync(7, default))!;
            reliability.RecordOverturn(ReliabilityCalculator);
            repository.Update(reliability);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
        }

        var loaded = (await LoadReliabilityAsync(7))!;
        Assert.Equal(1, loaded.OverturnedDecisionsCount);
        Assert.Equal(85, loaded.Score.Value);
    }

    [Fact]
    public async Task Reliability_TwoForTheSameVerifier_ViolateTheUniqueIndex()
    {
        await SaveAsync<IVerifierReliabilityRepository, VerifierReliability>(new VerifierReliability(7));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            SaveAsync<IVerifierReliabilityRepository, VerifierReliability>(new VerifierReliability(7)));
    }

    // ---------- Student employability ----------

    [Fact]
    public async Task Employability_RoundTripsTheSkillsAndTheScore()
    {
        var score = new StudentEmployabilityScore(5)
            .RecordSkillVerified(EmployabilityCalculator)
            .RecordSkillVerified(EmployabilityCalculator)
            .RecordSkillVerified(EmployabilityCalculator);
        await SaveAsync<IStudentEmployabilityScoreRepository, StudentEmployabilityScore>(score);

        var loaded = await LoadEmployabilityAsync(5);

        Assert.NotNull(loaded);
        Assert.True(loaded.Id > 0);
        Assert.Equal(3, loaded.VerifiedSkillsCount);
        Assert.Equal(30, loaded.Score.Value);
        Assert.Equal(DateTimeKind.Utc, loaded.UpdatedAt.Kind);
    }

    [Fact]
    public async Task Employability_FindByStudentId_WithAnUnknownStudent_ReturnsNull()
    {
        Assert.Null(await LoadEmployabilityAsync(99));
    }

    [Fact]
    public async Task Employability_Changes_ArePersisted()
    {
        await SaveAsync<IStudentEmployabilityScoreRepository, StudentEmployabilityScore>(
            new StudentEmployabilityScore(5));

        using (var scope = TestApi.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IStudentEmployabilityScoreRepository>();
            var score = (await repository.FindByStudentIdAsync(5, default))!;
            score.RecordSkillVerified(EmployabilityCalculator);
            repository.Update(score);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
        }

        var loaded = (await LoadEmployabilityAsync(5))!;
        Assert.Equal(1, loaded.VerifiedSkillsCount);
        Assert.Equal(10, loaded.Score.Value);
    }

    [Fact]
    public async Task Employability_TwoForTheSameStudent_ViolateTheUniqueIndex()
    {
        await SaveAsync<IStudentEmployabilityScoreRepository, StudentEmployabilityScore>(
            new StudentEmployabilityScore(5));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            SaveAsync<IStudentEmployabilityScoreRepository, StudentEmployabilityScore>(
                new StudentEmployabilityScore(5)));
    }
}