using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.RecognitionIncentives.Domain;

public class WalletTests
{
    [Fact]
    public void Constructor_StartsWithAnEmptyBalance()
    {
        var wallet = new Wallet(3);

        Assert.Equal(3, wallet.WalletOwnerId);
        Assert.Equal(0, wallet.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithAnInvalidOwner_ThrowsDomainException(int ownerId)
    {
        Assert.Throws<DomainException>(() => new Wallet(ownerId));
    }

    [Fact]
    public void Credit_AddsToTheBalance()
    {
        var wallet = new Wallet(3).Credit(new Credits(10)).Credit(new Credits(25));

        Assert.Equal(35, wallet.Balance);
    }

    [Fact]
    public void Credit_WithZero_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new Wallet(3).Credit(new Credits(0)));
    }

    [Fact]
    public void Credit_PastTheMaximumBalance_ThrowsDomainExceptionAndKeepsTheBalance()
    {
        var wallet = new Wallet(3).Credit(new Credits(int.MaxValue));

        Assert.Throws<DomainException>(() => wallet.Credit(new Credits(1)));
        Assert.Equal(int.MaxValue, wallet.Balance);
    }

    [Fact]
    public void Credit_WithoutAnAmount_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Wallet(3).Credit(null!));
    }

    [Fact]
    public void Debit_SubtractsFromTheBalance()
    {
        var wallet = new Wallet(3).Credit(new Credits(50)).Debit(new Credits(30));

        Assert.Equal(20, wallet.Balance);
    }

    [Fact]
    public void Debit_OfTheWholeBalance_LeavesItAtZero()
    {
        var wallet = new Wallet(3).Credit(new Credits(30)).Debit(new Credits(30));

        Assert.Equal(0, wallet.Balance);
    }

    [Fact]
    public void Debit_MoreThanTheBalance_ThrowsDomainExceptionAndKeepsTheBalance()
    {
        var wallet = new Wallet(3).Credit(new Credits(20));

        Assert.Throws<DomainException>(() => wallet.Debit(new Credits(21)));
        Assert.Equal(20, wallet.Balance);
    }

    [Fact]
    public void Debit_WithZero_ThrowsDomainException()
    {
        var wallet = new Wallet(3).Credit(new Credits(20));

        Assert.Throws<DomainException>(() => wallet.Debit(new Credits(0)));
    }

    [Fact]
    public void Debit_FromAnEmptyWallet_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new Wallet(3).Debit(new Credits(1)));
    }

    [Theory]
    [InlineData(19, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public void CanAfford_IsTrueWhenTheBalanceCoversTheAmount(int amount, bool expected)
    {
        var wallet = new Wallet(3).Credit(new Credits(20));

        Assert.Equal(expected, wallet.CanAfford(new Credits(amount)));
    }
}