using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.RecognitionIncentives.Domain.Repositories;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.RecognitionIncentives.Integration;

public class RecognitionPersistenceTests : ApiTestBase
{
    private static async Task SaveAsync<TRepository, TEntity>(TEntity entity)
        where TRepository : class, IBaseRepository<TEntity>
        where TEntity : class
    {
        using var scope = TestApi.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TRepository>().AddAsync(entity);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
    }

    private static async Task<Wallet> SaveWalletAsync(int ownerId, int balance = 0)
    {
        var wallet = new Wallet(ownerId);
        if (balance > 0) wallet.Credit(new Credits(balance));
        await SaveAsync<IWalletRepository, Wallet>(wallet);
        return wallet;
    }

    private static async Task<Wallet?> LoadWalletAsync(int ownerId)
    {
        using var scope = TestApi.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IWalletRepository>()
            .FindByOwnerIdAsync(ownerId, default);
    }

    private static Task AddTransactionAsync(Wallet wallet, int amount, TransactionType type, string description,
        int? caseId = null)
    {
        return SaveAsync<ICreditTransactionRepository, CreditTransaction>(
            new CreditTransaction(wallet.Id, new Credits(amount), type, description, caseId));
    }

    // ---------- Wallets ----------

    [Fact]
    public async Task Wallet_RoundTripsTheOwnerAndTheBalance()
    {
        await SaveWalletAsync(7, 35);

        var loaded = await LoadWalletAsync(7);

        Assert.NotNull(loaded);
        Assert.True(loaded.Id > 0);
        Assert.Equal(7, loaded.WalletOwnerId);
        Assert.Equal(35, loaded.Balance);
    }

    [Fact]
    public async Task Wallet_FindByOwnerId_WithAnUnknownUser_ReturnsNull()
    {
        Assert.Null(await LoadWalletAsync(99));
    }

    [Fact]
    public async Task Wallet_BalanceChanges_ArePersisted()
    {
        await SaveWalletAsync(7, 50);

        using (var scope = TestApi.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IWalletRepository>();
            var wallet = (await repository.FindByOwnerIdAsync(7, default))!;
            wallet.Debit(new Credits(30));
            repository.Update(wallet);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
        }

        Assert.Equal(20, (await LoadWalletAsync(7))!.Balance);
    }

    [Fact]
    public async Task Wallet_TwoForTheSameUser_ViolateTheUniqueIndex()
    {
        await SaveWalletAsync(7);

        await Assert.ThrowsAsync<DbUpdateException>(() => SaveWalletAsync(7));
    }

    [Fact]
    public async Task Wallet_TwoChangesAtTheSameTime_TheSecondOneFailsAndTheFirstOneStays()
    {
        await SaveWalletAsync(7, 100);

        using var first = TestApi.CreateScope();
        using var second = TestApi.CreateScope();
        var firstRepository = first.ServiceProvider.GetRequiredService<IWalletRepository>();
        var secondRepository = second.ServiceProvider.GetRequiredService<IWalletRepository>();
        var seenByFirst = (await firstRepository.FindByOwnerIdAsync(7, default))!;
        var seenBySecond = (await secondRepository.FindByOwnerIdAsync(7, default))!;

        seenByFirst.Debit(new Credits(60));
        firstRepository.Update(seenByFirst);
        await first.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();

        seenBySecond.Debit(new Credits(60));
        secondRepository.Update(seenBySecond);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            second.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync());

        Assert.Equal(40, (await LoadWalletAsync(7))!.Balance);
    }

    // ---------- Transactions ----------

    [Fact]
    public async Task Transaction_RoundTripsEveryField()
    {
        var wallet = await SaveWalletAsync(7);
        await AddTransactionAsync(wallet, 10, TransactionType.Earned, "Verification case resolved", 55);

        using var scope = TestApi.CreateScope();
        var found = await scope.ServiceProvider.GetRequiredService<ICreditTransactionRepository>()
            .FindByWalletIdAsync(wallet.Id, default);

        var transaction = Assert.Single(found);
        Assert.True(transaction.Id > 0);
        Assert.Equal(wallet.Id, transaction.WalletId);
        Assert.Equal(new Credits(10), transaction.Amount);
        Assert.Equal(TransactionType.Earned, transaction.Type);
        Assert.Equal("Verification case resolved", transaction.Description);
        Assert.Equal(55, transaction.RelatedCaseId);
        Assert.Equal(DateTimeKind.Utc, transaction.CreatedAt.Kind);
    }

    [Fact]
    public async Task Transaction_Redeemed_HasNoRelatedCase()
    {
        var wallet = await SaveWalletAsync(7);
        await AddTransactionAsync(wallet, 30, TransactionType.Redeemed, "Redeemed: contribution certificate");

        using var scope = TestApi.CreateScope();
        var transaction = Assert.Single(await scope.ServiceProvider.GetRequiredService<ICreditTransactionRepository>()
            .FindByWalletIdAsync(wallet.Id, default));

        Assert.Equal(TransactionType.Redeemed, transaction.Type);
        Assert.Null(transaction.RelatedCaseId);
    }

    [Fact]
    public async Task Transaction_FindByWalletId_ReturnsOnlyThatWalletNewestFirst()
    {
        var mine = await SaveWalletAsync(7);
        var other = await SaveWalletAsync(8);
        await AddTransactionAsync(mine, 10, TransactionType.Earned, "First", 1);
        await AddTransactionAsync(other, 10, TransactionType.Earned, "Not mine", 1);
        await AddTransactionAsync(mine, 10, TransactionType.Earned, "Second", 2);
        await AddTransactionAsync(mine, 30, TransactionType.Redeemed, "Last");

        using var scope = TestApi.CreateScope();
        var found = await scope.ServiceProvider.GetRequiredService<ICreditTransactionRepository>()
            .FindByWalletIdAsync(mine.Id, default);

        Assert.Equal(["Last", "Second", "First"], found.Select(t => t.Description));
    }

    [Fact]
    public async Task Transaction_ExistsEarnedForCase_IsTrueOnlyForThatWalletAndCase()
    {
        var mine = await SaveWalletAsync(7);
        var other = await SaveWalletAsync(8);
        await AddTransactionAsync(mine, 10, TransactionType.Earned, "Case resolved", 55);

        using var scope = TestApi.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICreditTransactionRepository>();
        Assert.True(await repository.ExistsEarnedForCaseAsync(mine.Id, 55, default));
        Assert.False(await repository.ExistsEarnedForCaseAsync(mine.Id, 56, default));
        Assert.False(await repository.ExistsEarnedForCaseAsync(other.Id, 55, default));
    }

    [Fact]
    public async Task Transaction_TheSameCaseTwiceInAWallet_ViolatesTheUniqueIndex()
    {
        var wallet = await SaveWalletAsync(7);
        await AddTransactionAsync(wallet, 10, TransactionType.Earned, "Case resolved", 55);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            AddTransactionAsync(wallet, 10, TransactionType.Earned, "Case resolved", 55));
    }

    [Fact]
    public async Task Transaction_TheSameCaseInTwoWallets_IsAllowed()
    {
        var mine = await SaveWalletAsync(7);
        var other = await SaveWalletAsync(8);
        await AddTransactionAsync(mine, 10, TransactionType.Earned, "Case resolved", 55);

        await AddTransactionAsync(other, 10, TransactionType.Earned, "Case resolved", 55);
    }

    [Fact]
    public async Task Transaction_SeveralWithoutACase_AreAllowed()
    {
        var wallet = await SaveWalletAsync(7, 100);
        await AddTransactionAsync(wallet, 30, TransactionType.Redeemed, "Redeemed: contribution certificate");

        await AddTransactionAsync(wallet, 30, TransactionType.Redeemed, "Redeemed: contribution certificate");
    }

    [Fact]
    public async Task Transaction_ForAWalletThatDoesNotExist_ViolatesTheForeignKey()
    {
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            SaveAsync<ICreditTransactionRepository, CreditTransaction>(
                new CreditTransaction(9999, new Credits(10), TransactionType.Earned, "Case resolved", 1)));
    }
}