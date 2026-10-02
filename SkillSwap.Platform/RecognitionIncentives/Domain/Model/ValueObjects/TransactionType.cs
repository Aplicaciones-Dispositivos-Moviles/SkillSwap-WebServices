namespace SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;

/// <summary>
///     The kind of movement of a wallet.
/// </summary>
public enum TransactionType
{
    /// <summary>
    ///     Credits earned by resolving a verification case.
    /// </summary>
    Earned,

    /// <summary>
    ///     Credits spent on a benefit.
    /// </summary>
    Redeemed
}