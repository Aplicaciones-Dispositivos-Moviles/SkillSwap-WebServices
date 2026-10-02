using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.RecognitionIncentives.Domain;

public class CreditTransactionTests
{
    [Fact]
    public void Constructor_RecordsAnEarnedMovementWithTheCase()
    {
        var before = DateTime.UtcNow;

        var transaction = new CreditTransaction(2, new Credits(10), TransactionType.Earned,
            "  Verification case resolved  ", 7);

        Assert.Equal(2, transaction.WalletId);
        Assert.Equal(new Credits(10), transaction.Amount);
        Assert.Equal(TransactionType.Earned, transaction.Type);
        Assert.Equal("Verification case resolved", transaction.Description);
        Assert.Equal(7, transaction.RelatedCaseId);
        Assert.InRange(transaction.CreatedAt, before, DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_RecordsARedeemedMovementWithoutACase()
    {
        var transaction = new CreditTransaction(2, new Credits(30), TransactionType.Redeemed,
            "Redeemed: contribution certificate");

        Assert.Equal(TransactionType.Redeemed, transaction.Type);
        Assert.Null(transaction.RelatedCaseId);
    }

    [Fact]
    public void Constructor_WithAnEarnedMovementWithoutACase_IsAllowed()
    {
        var transaction = new CreditTransaction(2, new Credits(10), TransactionType.Earned, "Bonus");

        Assert.Null(transaction.RelatedCaseId);
    }

    [Fact]
    public void Constructor_WithAZeroAmount_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new CreditTransaction(2, new Credits(0), TransactionType.Earned, "Nothing"));
    }

    [Fact]
    public void Constructor_WithoutAnAmount_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new CreditTransaction(2, null!, TransactionType.Earned, "Nothing"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithAnInvalidWallet_ThrowsDomainException(int walletId)
    {
        Assert.Throws<DomainException>(() =>
            new CreditTransaction(walletId, new Credits(10), TransactionType.Earned, "Case resolved"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithoutADescription_ThrowsDomainException(string description)
    {
        Assert.Throws<DomainException>(() =>
            new CreditTransaction(2, new Credits(10), TransactionType.Earned, description));
    }

    [Fact]
    public void Constructor_WithATooLongDescription_ThrowsDomainException()
    {
        var description = new string('a', CreditTransaction.MaxDescriptionLength + 1);

        Assert.Throws<DomainException>(() =>
            new CreditTransaction(2, new Credits(10), TransactionType.Earned, description));
    }

    [Fact]
    public void Constructor_WithAnUndefinedType_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new CreditTransaction(2, new Credits(10), (TransactionType)99, "Case resolved"));
    }

    [Fact]
    public void Constructor_WithACaseOnARedeemedMovement_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new CreditTransaction(2, new Credits(30), TransactionType.Redeemed, "Redeemed", 7));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithAnInvalidCase_ThrowsDomainException(int caseId)
    {
        Assert.Throws<DomainException>(() =>
            new CreditTransaction(2, new Credits(10), TransactionType.Earned, "Case resolved", caseId));
    }
}