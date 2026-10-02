using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Commands;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Transform;

public static class RedeemCommandFromResourceAssembler
{
    /// <remarks>
    ///     The user is always the authenticated one. A benefit that is missing or is not one of the known names
    ///     becomes an undefined value, which the service rejects as InvalidRedemptionItem.
    /// </remarks>
    public static RedeemCommand ToCommandFromResource(RedeemResource resource, int userId)
    {
        return new RedeemCommand(userId, ParseItem(resource.Item));
    }

    private static RedemptionItem ParseItem(string? value)
    {
        return Enum.TryParse<RedemptionItem>(value, true, out var item) && Enum.IsDefined(item)
            ? item
            : (RedemptionItem)(-1);
    }
}