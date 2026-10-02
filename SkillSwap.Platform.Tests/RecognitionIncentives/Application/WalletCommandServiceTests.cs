using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SkillSwap.Platform.RecognitionIncentives.Application.Internal.CommandServices;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Commands;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.RecognitionIncentives.Domain.Services;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Resources.Errors;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.RecognitionIncentives.Application;

public class WalletCommandServiceTests
{
    private readonly WalletCommandService _service;
    private readonly FakeCreditTransactionRepository _transactions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeWalletRepository _wallets = new();

    public WalletCommandServiceTests()
    {
        _service = new WalletCommandService(
            _wallets,
            _transactions,
            new RedemptionPricing(),
            _unitOfWork,
            new FakeLocalizer<ErrorMessage>(),
            NullLogger<WalletCommandService>.Instance);
    }

    private async Task<Wallet> WalletWithAsync(int ownerId, int balance)
    {
        var wallet = new Wallet(ownerId);
        if (balance > 0) wallet.Credit(new Credits(balance));
        await _wallets.AddAsync(wallet);
        return wallet;
    }

    private Task<Result<Wallet>> CreditAsync(int verifierUserId = 2, int caseId = 77)
    {
        return _service.Handle(new CreditVerifierCommand(verifierUserId, caseId), CancellationToken.None);
    }

    private Task<Result<CreditTransaction>> RedeemAsync(RedemptionItem item, int userId = 2)
    {
        return _service.Handle(new RedeemCommand(userId, item), CancellationToken.None);
    }

    private static void AssertFailure<T>(Result<T> result, RecognitionIncentivesError expected)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expected, Assert.IsType<RecognitionIncentivesError>(result.Error));
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_MakesAnEmptyWalletForTheUser()
    {
        var result = await _service.Handle(new CreateWalletCommand(3), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var wallet = Assert.Single(_wallets.Items);
        Assert.Same(wallet, result.Value);
        Assert.Equal(3, wallet.WalletOwnerId);
        Assert.Equal(0, wallet.Balance);
    }

    [Fact]
    public async Task Create_ForAUserWhoAlreadyHasAWallet_ReturnsItUnchanged()
    {
        var existing = await WalletWithAsync(3, 40);

        var result = await _service.Handle(new CreateWalletCommand(3), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(existing, result.Value);
        Assert.Single(_wallets.Items);
        Assert.Equal(40, existing.Balance);
    }

    [Fact]
    public async Task Create_WithAnInvalidUser_FailsWithInternalServerErrorAndSavesNothing()
    {
        AssertFailure(await _service.Handle(new CreateWalletCommand(0), CancellationToken.None),
            RecognitionIncentivesError.InternalServerError);
        Assert.Empty(_wallets.Items);
    }

    [Fact]
    public async Task Create_WhenPersistenceFails_FailsWithDatabaseError()
    {
        _unitOfWork.ExceptionToThrow = new DbUpdateException("failure");

        AssertFailure(await _service.Handle(new CreateWalletCommand(3), CancellationToken.None),
            RecognitionIncentivesError.DatabaseError);
    }

    // ---------- Credit ----------

    [Fact]
    public async Task Credit_ForAVerifierWithoutAWallet_CreatesItAndPaysTheCase()
    {
        var result = await CreditAsync(verifierUserId: 2, caseId: 77);

        Assert.True(result.IsSuccess);
        var wallet = Assert.Single(_wallets.Items);
        Assert.Same(wallet, result.Value);
        Assert.Equal(2, wallet.WalletOwnerId);
        Assert.Equal(10, wallet.Balance);

        var transaction = Assert.Single(_transactions.Items);
        Assert.Equal(wallet.Id, transaction.WalletId);
        Assert.True(wallet.Id > 0);
        Assert.Equal(TransactionType.Earned, transaction.Type);
        Assert.Equal(10, transaction.Amount.Value);
        Assert.Equal(77, transaction.RelatedCaseId);
        Assert.Equal("Verification case resolved", transaction.Description);
    }

    [Fact]
    public async Task Credit_ForAnExistingWallet_AddsToTheBalance()
    {
        var wallet = await WalletWithAsync(2, 5);

        await CreditAsync(verifierUserId: 2, caseId: 77);

        Assert.Equal(15, wallet.Balance);
        Assert.Single(_wallets.Items);
        Assert.Single(_transactions.Items);
    }

    [Fact]
    public async Task Credit_SeveralCases_Accumulate()
    {
        await CreditAsync(caseId: 1);
        await CreditAsync(caseId: 2);
        await CreditAsync(caseId: 3);

        Assert.Equal(30, Assert.Single(_wallets.Items).Balance);
        Assert.Equal(3, _transactions.Items.Count);
    }

    [Fact]
    public async Task Credit_TheSameCaseTwice_PaysOnlyOnce()
    {
        await CreditAsync(caseId: 77);

        var second = await CreditAsync(caseId: 77);

        Assert.True(second.IsSuccess);
        Assert.Equal(10, Assert.Single(_wallets.Items).Balance);
        Assert.Single(_transactions.Items);
    }

    [Fact]
    public async Task Credit_KeepsTheWalletsOfDifferentVerifiersSeparate()
    {
        await CreditAsync(verifierUserId: 2, caseId: 1);
        await CreditAsync(verifierUserId: 3, caseId: 2);

        Assert.Equal(2, _wallets.Items.Count);
        Assert.All(_wallets.Items, wallet => Assert.Equal(10, wallet.Balance));
    }

    [Fact]
    public async Task Credit_WithAnInvalidCase_FailsWithInternalServerError()
    {
        AssertFailure(await CreditAsync(caseId: 0), RecognitionIncentivesError.InternalServerError);
        Assert.Empty(_transactions.Items);
    }

    [Fact]
    public async Task Credit_WithAnInvalidVerifier_FailsWithInternalServerErrorAndSavesNothing()
    {
        AssertFailure(await CreditAsync(verifierUserId: 0), RecognitionIncentivesError.InternalServerError);
        Assert.Empty(_wallets.Items);
        Assert.Empty(_transactions.Items);
    }

    [Fact]
    public async Task Credit_WhenPersistenceFails_FailsWithDatabaseError()
    {
        _unitOfWork.ExceptionToThrow = new DbUpdateException("failure");

        AssertFailure(await CreditAsync(), RecognitionIncentivesError.DatabaseError);
    }

    [Fact]
    public async Task Credit_WhenTheWalletWasModifiedAtTheSameTime_FailsWithConcurrentUpdate()
    {
        _unitOfWork.ExceptionToThrow = new DbUpdateConcurrencyException("conflict");

        AssertFailure(await CreditAsync(), RecognitionIncentivesError.ConcurrentUpdate);
    }

    // ---------- Redeem ----------

    [Theory]
    [InlineData(RedemptionItem.AdvancedPathUnlock, 50, "Redeemed: advanced path unlock")]
    [InlineData(RedemptionItem.ContributionCertificate, 30, "Redeemed: contribution certificate")]
    public async Task Redeem_WithEnoughBalance_TakesTheCostAndRecordsTheMovement(RedemptionItem item, int cost,
        string description)
    {
        var wallet = await WalletWithAsync(2, 100);

        var result = await RedeemAsync(item);

        Assert.True(result.IsSuccess);
        Assert.Equal(100 - cost, wallet.Balance);
        var transaction = Assert.Single(_transactions.Items);
        Assert.Same(transaction, result.Value);
        Assert.Equal(wallet.Id, transaction.WalletId);
        Assert.Equal(TransactionType.Redeemed, transaction.Type);
        Assert.Equal(cost, transaction.Amount.Value);
        Assert.Equal(description, transaction.Description);
        Assert.Null(transaction.RelatedCaseId);
    }

    [Fact]
    public async Task Redeem_WithExactlyTheCost_LeavesTheBalanceAtZero()
    {
        var wallet = await WalletWithAsync(2, 30);

        var result = await RedeemAsync(RedemptionItem.ContributionCertificate);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, wallet.Balance);
    }

    [Fact]
    public async Task Redeem_WithInsufficientBalance_FailsAndKeepsTheBalance()
    {
        var wallet = await WalletWithAsync(2, 29);

        AssertFailure(await RedeemAsync(RedemptionItem.ContributionCertificate),
            RecognitionIncentivesError.InsufficientBalance);

        Assert.Equal(29, wallet.Balance);
        Assert.Empty(_transactions.Items);
    }

    [Fact]
    public async Task Redeem_WithAnEmptyWallet_FailsWithInsufficientBalance()
    {
        await WalletWithAsync(2, 0);

        AssertFailure(await RedeemAsync(RedemptionItem.AdvancedPathUnlock),
            RecognitionIncentivesError.InsufficientBalance);
    }

    [Fact]
    public async Task Redeem_WithoutAWallet_FailsWithWalletNotFound()
    {
        AssertFailure(await RedeemAsync(RedemptionItem.AdvancedPathUnlock), RecognitionIncentivesError.WalletNotFound);
    }

    [Fact]
    public async Task Redeem_AnotherUsersBalance_IsNeverUsed()
    {
        await WalletWithAsync(3, 500);
        await WalletWithAsync(2, 0);

        AssertFailure(await RedeemAsync(RedemptionItem.AdvancedPathUnlock, userId: 2),
            RecognitionIncentivesError.InsufficientBalance);
    }

    [Fact]
    public async Task Redeem_WithAnUndefinedBenefit_FailsWithInvalidRedemptionItem()
    {
        var wallet = await WalletWithAsync(2, 100);

        AssertFailure(await RedeemAsync((RedemptionItem)99), RecognitionIncentivesError.InvalidRedemptionItem);

        Assert.Equal(100, wallet.Balance);
    }

    [Fact]
    public async Task Redeem_WhenPersistenceFails_FailsWithDatabaseError()
    {
        await WalletWithAsync(2, 100);
        _unitOfWork.ExceptionToThrow = new DbUpdateException("failure");

        AssertFailure(await RedeemAsync(RedemptionItem.ContributionCertificate),
            RecognitionIncentivesError.DatabaseError);
    }

    [Fact]
    public async Task Redeem_WhenTheWalletWasModifiedAtTheSameTime_FailsWithConcurrentUpdate()
    {
        await WalletWithAsync(2, 100);
        _unitOfWork.ExceptionToThrow = new DbUpdateConcurrencyException("conflict");

        AssertFailure(await RedeemAsync(RedemptionItem.ContributionCertificate),
            RecognitionIncentivesError.ConcurrentUpdate);
    }

    [Fact]
    public async Task Redeem_WhenTheRequestIsCancelled_FailsWithOperationCancelled()
    {
        await WalletWithAsync(2, 100);
        _unitOfWork.ExceptionToThrow = new OperationCanceledException();

        AssertFailure(await RedeemAsync(RedemptionItem.ContributionCertificate),
            RecognitionIncentivesError.OperationCancelled);
    }
}