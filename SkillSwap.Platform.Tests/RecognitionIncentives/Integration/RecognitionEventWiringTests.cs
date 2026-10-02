using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Events;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Domain.Model.Events;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.RecognitionIncentives.Application.EventHandlers;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.RecognitionIncentives.Domain.Repositories;
using SkillSwap.Platform.Reputation.Application.EventHandlers;
using SkillSwap.Platform.Shared.Domain.Events;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.RecognitionIncentives.Integration;

/// <summary>
///     Publishes events through the real publisher, and also drives the real API, to check that the wallet reacts to
///     the registration of a user and to the resolution of a case.
/// </summary>
public class RecognitionEventWiringTests : ApiTestBase
{
    private static async Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        using var scope = TestApi.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>()
            .PublishAsync(domainEvent, default);
    }

    private static async Task<Wallet?> WalletOfAsync(int ownerId)
    {
        using var scope = TestApi.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IWalletRepository>()
            .FindByOwnerIdAsync(ownerId, default);
    }

    private static async Task<IReadOnlyList<CreditTransaction>> TransactionsOfAsync(int ownerId)
    {
        using var scope = TestApi.CreateScope();
        var wallet = await scope.ServiceProvider.GetRequiredService<IWalletRepository>()
            .FindByOwnerIdAsync(ownerId, default);
        return await scope.ServiceProvider.GetRequiredService<ICreditTransactionRepository>()
            .FindByWalletIdAsync(wallet!.Id, default);
    }

    private static VerificationCaseResolved Resolved(int caseId, ReviewDecision decision = ReviewDecision.Approved,
        int verifierUserId = 2)
    {
        return new VerificationCaseResolved(caseId, 7, verifierUserId, 5, "http-basics", decision);
    }

    // ---------- Registration ----------

    [Fact]
    public void TheHandlersOfBothEvents_AreRegistered()
    {
        using var scope = TestApi.CreateScope();
        var provider = scope.ServiceProvider;

        Assert.Contains(provider.GetServices<IDomainEventHandler<UserRegistered>>(),
            handler => handler is CreateWalletEventHandler);

        // The case resolution has two independent reactions: the wallet and the reputation.
        var resolution = provider.GetServices<IDomainEventHandler<VerificationCaseResolved>>().ToList();
        Assert.Contains(resolution, handler => handler is CreditVerifierEventHandler);
        Assert.Contains(resolution, handler => handler is RecordCaseResolutionEventHandler);
    }

    [Fact]
    public async Task SigningUp_CreatesAnEmptyWalletForTheNewUser()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");

        var wallet = await WalletOfAsync(ana.Id);

        Assert.NotNull(wallet);
        Assert.Equal(0, wallet.Balance);
        Assert.Empty(await TransactionsOfAsync(ana.Id));
    }

    [Fact]
    public async Task EachUser_GetsTheirOwnWallet()
    {
        var ana = await TestApi.RegisterStudentAsync("ana");
        var bob = await TestApi.RegisterStudentAsync("bob");

        Assert.NotEqual((await WalletOfAsync(ana.Id))!.Id, (await WalletOfAsync(bob.Id))!.Id);
    }

    [Fact]
    public async Task RegisteringTheSameUserEventTwice_KeepsASingleWallet()
    {
        await PublishAsync(new UserRegistered(7, UserRole.Student));
        await PublishAsync(new UserRegistered(7, UserRole.Student));

        Assert.NotNull(await WalletOfAsync(7));
    }

    // ---------- Credits ----------

    [Theory]
    [InlineData(ReviewDecision.Approved)]
    [InlineData(ReviewDecision.Rejected)]
    public async Task CaseResolved_CreditsTenCreditsWhateverTheDecision(ReviewDecision decision)
    {
        await PublishAsync(Resolved(55, decision));

        Assert.Equal(10, (await WalletOfAsync(2))!.Balance);
        var transaction = Assert.Single(await TransactionsOfAsync(2));
        Assert.Equal(TransactionType.Earned, transaction.Type);
        Assert.Equal(55, transaction.RelatedCaseId);
    }

    [Fact]
    public async Task CaseResolved_ForAWalletCreatedAtRegistration_AddsToIt()
    {
        var bob = await TestApi.RegisterStudentAsync("bob");

        await PublishAsync(Resolved(55, verifierUserId: bob.Id));
        await PublishAsync(Resolved(56, verifierUserId: bob.Id));

        Assert.Equal(20, (await WalletOfAsync(bob.Id))!.Balance);
        Assert.Equal(2, (await TransactionsOfAsync(bob.Id)).Count);
    }

    [Fact]
    public async Task CaseResolved_PublishedTwice_PaysOnlyOnce()
    {
        await PublishAsync(Resolved(55));
        await PublishAsync(Resolved(55));

        Assert.Equal(10, (await WalletOfAsync(2))!.Balance);
        Assert.Single(await TransactionsOfAsync(2));
    }

    [Fact]
    public async Task ResolvingACaseThroughTheApi_CreditsTheVerifier()
    {
        var bob = await AssessmentFlow.RegisterVerifierAsync("bob");
        var ana = await TestApi.RegisterStudentAsync("ana");
        await AssessmentFlow.DeclareGoalAsync(ana);
        var (_, attempt) = await AssessmentFlow.FailSkillAsync(ana, "networking-basics");
        var caseId = attempt.VerificationCaseId!.Value;

        var response = await AssessmentFlow.ResolveAsync(bob, caseId, "Rejected", "The explanation is incomplete.");

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(10, (await WalletOfAsync(bob.Id))!.Balance);
        var transaction = Assert.Single(await TransactionsOfAsync(bob.Id));
        Assert.Equal(caseId, transaction.RelatedCaseId);
        Assert.Equal(0, (await WalletOfAsync(ana.Id))!.Balance);
    }
}