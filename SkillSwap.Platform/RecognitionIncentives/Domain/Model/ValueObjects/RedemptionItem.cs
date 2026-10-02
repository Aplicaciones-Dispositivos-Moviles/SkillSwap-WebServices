namespace SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;

/// <summary>
///     The benefits that can be redeemed with SkillCredits.
/// </summary>
public enum RedemptionItem
{
    /// <summary>
    ///     Unlocks an advanced node of the learning path.
    /// </summary>
    AdvancedPathUnlock,

    /// <summary>
    ///     An exportable certificate of the contribution as a verifier.
    /// </summary>
    ContributionCertificate
}