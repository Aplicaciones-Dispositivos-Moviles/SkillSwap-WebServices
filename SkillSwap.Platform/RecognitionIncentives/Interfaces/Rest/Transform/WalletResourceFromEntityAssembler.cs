using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Transform;

public static class WalletResourceFromEntityAssembler
{
    public static WalletResource ToResourceFromEntity(Wallet entity)
    {
        return new WalletResource(entity.Id, entity.WalletOwnerId, entity.Balance);
    }
}