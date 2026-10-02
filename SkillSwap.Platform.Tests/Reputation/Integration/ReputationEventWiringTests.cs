using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Events;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;
using SkillSwap.Platform.Reputation.Application.EventHandlers;
using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Repositories;
using SkillSwap.Platform.Shared.Domain.Events;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Reputation.Integration;

/// <summary>
///     Publishes the events of Assessment &amp; Peer Review through the real publisher and checks that Reputation
///     reacts to them.
/// </summary>
public class ReputationEventWiringTests : ApiTestBase
{
    private static async Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        using var scope = TestApi.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>()
            .PublishAsync(domainEvent, default);
    }

    private static async Task<VerifierReliability?> ReliabilityOfAsync(int verifierUserId)
    {
        using var scope = TestApi.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IVerifierReliabilityRepository>()
            .FindByVerifierUserIdAsync(verifierUserId, default);
    }

    private static async Task<StudentEmployabilityScore?> EmployabilityOfAsync(int studentId)
    {
        using var scope = TestApi.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IStudentEmployabilityScoreRepository>()
            .FindByStudentIdAsync(studentId, default);
    }

    private static VerificationCaseResolved Resolved(ReviewDecision decision, int verifierUserId = 2,
        int studentId = 7)
    {
        return new VerificationCaseResolved(10, studentId, verifierUserId, 5, "http-basics", decision);
    }

    [Fact]
    public void TheHandlersOfBothEvents_AreRegistered()
    {
        using var scope = TestApi.CreateScope();
        var provider = scope.ServiceProvider;

        Assert.Contains(provider.GetServices<IDomainEventHandler<AssessmentAttemptPassed>>(),
            handler => handler is RecordAutomaticApprovalEventHandler);
        Assert.Contains(provider.GetServices<IDomainEventHandler<VerificationCaseResolved>>(),
            handler => handler is RecordCaseResolutionEventHandler);
    }

    [Fact]
    public async Task AttemptPassed_CertifiesTheSkillOfTheStudent()
    {
        await PublishAsync(new AssessmentAttemptPassed(1, 7, 5, "http-basics"));

        var employability = await EmployabilityOfAsync(7);
        Assert.NotNull(employability);
        Assert.Equal(1, employability.VerifiedSkillsCount);
        Assert.Equal(10, employability.Score.Value);
    }

    [Fact]
    public async Task CaseApproved_RecordsTheResolutionAndCertifiesTheSkill()
    {
        await PublishAsync(Resolved(ReviewDecision.Approved));

        var reliability = await ReliabilityOfAsync(2);
        Assert.NotNull(reliability);
        Assert.Equal(1, reliability.ResolvedCasesCount);
        Assert.Equal(100, reliability.Score.Value);
        Assert.Equal(1, (await EmployabilityOfAsync(7))!.VerifiedSkillsCount);
    }

    [Fact]
    public async Task CaseRejected_RecordsTheResolutionButCertifiesNothing()
    {
        await PublishAsync(Resolved(ReviewDecision.Rejected));

        Assert.Equal(1, (await ReliabilityOfAsync(2))!.ResolvedCasesCount);
        Assert.Null(await EmployabilityOfAsync(7));
    }

    [Fact]
    public async Task SeveralEvents_Accumulate()
    {
        await PublishAsync(new AssessmentAttemptPassed(1, 7, 5, "http-basics"));
        await PublishAsync(Resolved(ReviewDecision.Approved));
        await PublishAsync(Resolved(ReviewDecision.Rejected, studentId: 8));

        Assert.Equal(2, (await ReliabilityOfAsync(2))!.ResolvedCasesCount);
        Assert.Equal(2, (await EmployabilityOfAsync(7))!.VerifiedSkillsCount);
        Assert.Null(await EmployabilityOfAsync(8));
    }

    [Fact]
    public async Task CaseResolved_UpdatesTheRatingOfTheVerifierProfile()
    {
        using (var scope = TestApi.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IVerifierProfileRepository>()
                .AddAsync(new VerifierProfile(2, "http-basics"));
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
        }

        await PublishAsync(Resolved(ReviewDecision.Approved));

        using var check = TestApi.CreateScope();
        var profile = await check.ServiceProvider.GetRequiredService<IVerifierProfileRepository>()
            .FindByUserIdAsync(2, default);
        Assert.Equal(100, profile!.Rating);
    }

    [Fact]
    public async Task CaseResolved_ForAUserWithoutAVerifierProfile_StillRecordsTheReputation()
    {
        await PublishAsync(Resolved(ReviewDecision.Approved));

        Assert.NotNull(await ReliabilityOfAsync(2));
    }
}