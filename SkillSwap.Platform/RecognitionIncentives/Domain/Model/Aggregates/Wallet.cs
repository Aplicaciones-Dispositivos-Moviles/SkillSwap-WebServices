using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;

/// <summary>
///     Wallet aggregate root
/// </summary>
/// <remarks>
///     The SkillCredits balance of a user. There is no real money in it: the credits are earned by verifying and
///     can only be spent on benefits of the platform. The movements are recorded as
///     <see cref="Entities.CreditTransaction" />.
/// </remarks>
public class Wallet
{
    /// <summary>
    ///     Parameterless constructor required by EF Core.
    /// </summary>
    protected Wallet()
    {
    }

    public Wallet(int walletOwnerId)
    {
        if (walletOwnerId <= 0)
            throw new DomainException("The wallet must belong to a valid user.");

        WalletOwnerId = walletOwnerId;
        Balance = 0;
    }

    public int Id { get; private set; }
    public int WalletOwnerId { get; private set; }
    public int Balance { get; private set; }

    /// <summary>
    ///     Whether the balance covers the amount.
    /// </summary>
    public bool CanAfford(Credits amount)
    {
        ArgumentNullException.ThrowIfNull(amount);
        return Balance >= amount.Value;
    }

    /// <summary>
    ///     Adds earned credits to the balance.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the amount is not positive or the balance would overflow.</exception>
    public Wallet Credit(Credits amount)
    {
        ArgumentNullException.ThrowIfNull(amount);
        if (!amount.IsPositive)
            throw new DomainException("The amount to credit must be positive.");
        if ((long)Balance + amount.Value > int.MaxValue)
            throw new DomainException("The balance cannot exceed the maximum supported.");

        Balance += amount.Value;
        return this;
    }

    /// <summary>
    ///     Takes redeemed credits from the balance.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the amount is not positive or the balance does not cover it.</exception>
    public Wallet Debit(Credits amount)
    {
        ArgumentNullException.ThrowIfNull(amount);
        if (!amount.IsPositive)
            throw new DomainException("The amount to debit must be positive.");
        if (!CanAfford(amount))
            throw new DomainException("The balance is not enough for this amount.");

        Balance -= amount.Value;
        return this;
    }
}