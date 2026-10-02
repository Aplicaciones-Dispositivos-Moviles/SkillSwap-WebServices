using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Transform;

public static class CreditTransactionResourceFromEntityAssembler
{
    public static CreditTransactionResource ToResourceFromEntity(CreditTransaction entity)
    {
        return new CreditTransactionResource(
            entity.Id,
            entity.WalletId,
            entity.Amount.Value,
            entity.Type.ToString(),
            entity.Description,
            entity.RelatedCaseId,
            entity.CreatedAt);
    }
}