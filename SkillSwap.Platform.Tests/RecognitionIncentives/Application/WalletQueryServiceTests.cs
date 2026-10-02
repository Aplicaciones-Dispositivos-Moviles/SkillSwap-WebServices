using SkillSwap.Platform.RecognitionIncentives.Application.Internal.QueryServices;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Queries;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.RecognitionIncentives.Application;

public class WalletQueryServiceTests
{
    private readonly WalletQueryService _service;
    private readonly FakeCreditTransactionRepository _transactions = new();
    private readonly FakeWalletRepository _wallets = new();

    public WalletQueryServiceTests()
    {
        _service = new WalletQueryService(_wallets, _transactions);
    }

    [Fact]
    public async Task WalletQuery_ReturnsTheWalletOfTheUserOrNull()
    {
        var wallet = new Wallet(2);
        await _wallets.AddAsync(wallet);

        Assert.Same(wallet, await _service.Handle(new GetWalletByOwnerIdQuery(2), CancellationToken.None));
        Assert.Null(await _service.Handle(new GetWalletByOwnerIdQuery(3), CancellationToken.None));
    }

    [Fact]
    public async Task TransactionsQuery_ReturnsTheMovementsOfThatWalletNewestFirst()
    {
        var mine = new Wallet(2);
        var other = new Wallet(3);
        await _wallets.AddAsync(mine);
        await _wallets.AddAsync(other);
        var first = new CreditTransaction(mine.Id, new Credits(10), TransactionType.Earned, "Case resolved", 1);
        var second = new CreditTransaction(mine.Id, new Credits(10), TransactionType.Earned, "Case resolved", 2);
        var spent = new CreditTransaction(mine.Id, new Credits(30), TransactionType.Redeemed, "Redeemed");
        var someoneElses = new CreditTransaction(other.Id, new Credits(10), TransactionType.Earned, "Case resolved", 3);
        foreach (var transaction in new[] { first, second, someoneElses, spent })
            await _transactions.AddAsync(transaction);

        var found = await _service.Handle(new GetWalletTransactionsQuery(2), CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal([spent.Id, second.Id, first.Id], found.Select(t => t.Id));
    }

    [Fact]
    public async Task TransactionsQuery_ForAWalletWithoutMovements_ReturnsAnEmptyList()
    {
        await _wallets.AddAsync(new Wallet(2));

        var found = await _service.Handle(new GetWalletTransactionsQuery(2), CancellationToken.None);

        Assert.NotNull(found);
        Assert.Empty(found);
    }

    [Fact]
    public async Task TransactionsQuery_ForAUserWithoutAWallet_ReturnsNull()
    {
        Assert.Null(await _service.Handle(new GetWalletTransactionsQuery(2), CancellationToken.None));
    }
}