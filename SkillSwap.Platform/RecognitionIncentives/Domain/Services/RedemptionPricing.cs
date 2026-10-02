using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.RecognitionIncentives.Domain.Services;

/// <summary>
///     Advanced path unlock = 50 SkillCredits; contribution certificate = 30 SkillCredits.
/// </summary>
public class RedemptionPricing : IRedemptionPricing
{
    public const int AdvancedPathUnlockCost = 50;
    public const int ContributionCertificateCost = 30;

    /// <inheritdoc />
    public Credits CalculateCost(RedemptionItem item)
    {
        return item switch
        {
            RedemptionItem.AdvancedPathUnlock => new Credits(AdvancedPathUnlockCost),
            RedemptionItem.ContributionCertificate => new Credits(ContributionCertificateCost),
            _ => throw new DomainException("The benefit to redeem is not valid.")
        };
    }
}