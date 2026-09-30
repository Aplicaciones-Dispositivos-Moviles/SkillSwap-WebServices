using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.Iam.Interfaces.Rest.Transform;

public static class PublicUserResourceFromEntityAssembler
{
    public static PublicUserResource ToResourceFromEntity(User entity)
    {
        return new PublicUserResource(
            entity.Id,
            entity.Username.Value,
            entity.Role.ToString(),
            entity.IsVerified,
            entity.Bio);
    }
}