using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.RecognitionIncentives.Domain.Services;

/// <summary>
///     What a verifier earns. Every resolved case pays the same, whether it was approved or rejected, so there
///     is no incentive to decide in one direction.
/// </summary>
public static class CreditRewards
{
    public const int PerResolvedCase = 10;

    public static Credits ForResolvedCase()
    {
        return new Credits(PerResolvedCase);
    }
}