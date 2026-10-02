using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.RecognitionIncentives.Domain.Model.Commands;

/// <summary>
///     Redeem command
/// </summary>
/// <param name="UserId">The authenticated user who redeems (taken from the token, never from the body)</param>
/// <param name="Item">The benefit to redeem</param>
public record RedeemCommand(int UserId, RedemptionItem Item);