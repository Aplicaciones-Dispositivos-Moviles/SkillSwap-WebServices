using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;

/// <summary>
///     CreditTransaction entity
/// </summary>
/// <remarks>
///     A movement of a wallet: credits earned by resolving a case or credits redeemed for a benefit. A transaction
///     never changes once it was recorded. An earned transaction can point to the case that originated it, which
///     keeps a case from crediting twice.
/// </remarks>
public class CreditTransaction
{
    public const int MaxDescriptionLength = 200;

    /// <summary>
    ///     Parameterless constructor required by EF Core.
    /// </summary>
    protected CreditTransaction()
    {
        Amount = null!;
        Description = null!;
    }

    public CreditTransaction(int walletId, Credits amount, TransactionType type, string description,
        int? relatedCaseId = null)
    {
        if (walletId <= 0)
            throw new DomainException("The transaction must belong to a valid wallet.");
        if (amount is null || !amount.IsPositive)
            throw new DomainException("The amount of a transaction must be positive.");
        if (!Enum.IsDefined(type))
            throw new DomainException("The type of the transaction is not valid.");

        var text = description?.Trim() ?? string.Empty;
        if (text.Length == 0)
            throw new DomainException("The description of the transaction cannot be empty.");
        if (text.Length > MaxDescriptionLength)
            throw new DomainException($"The description cannot exceed {MaxDescriptionLength} characters.");

        if (relatedCaseId is not null)
        {
            if (type != TransactionType.Earned)
                throw new DomainException("Only an earned transaction can point to a verification case.");
            if (relatedCaseId <= 0)
                throw new DomainException("The related case must be valid.");
        }

        WalletId = walletId;
        Amount = amount;
        Type = type;
        Description = text;
        RelatedCaseId = relatedCaseId;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int WalletId { get; private set; }
    public Credits Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public string Description { get; private set; }

    /// <summary>
    ///     The verification case that originated an earned transaction, if any.
    /// </summary>
    public int? RelatedCaseId { get; private set; }

    public DateTime CreatedAt { get; private set; }
}