namespace SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Resources;

/// <summary>
///     Resource for redeeming a benefit with SkillCredits
/// </summary>
/// <param name="Item">AdvancedPathUnlock (50 SkillCredits) or ContributionCertificate (30 SkillCredits)</param>
public record RedeemResource(string? Item);