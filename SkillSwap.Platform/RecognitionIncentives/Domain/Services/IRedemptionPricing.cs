using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.RecognitionIncentives.Domain.Services;

/// <summary>
///     Defines how many SkillCredits each benefit costs, apart from the wallet.
/// </summary>
public interface IRedemptionPricing
{
    Credits CalculateCost(RedemptionItem item);
}