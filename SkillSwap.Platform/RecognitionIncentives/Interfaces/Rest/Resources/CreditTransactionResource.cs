namespace SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Resources;

/// <summary>
///     Credit transaction resource for REST API
/// </summary>
/// <param name="Id">The unique identifier of the movement</param>
/// <param name="WalletId">The wallet the movement belongs to</param>
/// <param name="Amount">The SkillCredits of the movement, always positive</param>
/// <param name="Type">Earned (credits gained by resolving a case) or Redeemed (credits spent on a benefit)</param>
/// <param name="Description">What the movement was for</param>
/// <param name="RelatedCaseId">The verification case that paid an earned movement, if any</param>
/// <param name="CreatedAt">When the movement was recorded (UTC)</param>
public record CreditTransactionResource(
    int Id,
    int WalletId,
    int Amount,
    string Type,
    string Description,
    int? RelatedCaseId,
    DateTime CreatedAt);