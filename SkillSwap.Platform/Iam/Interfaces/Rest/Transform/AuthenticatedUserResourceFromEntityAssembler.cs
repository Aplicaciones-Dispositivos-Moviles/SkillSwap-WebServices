using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.Iam.Interfaces.Rest.Transform;

public static class AuthenticatedUserResourceFromEntityAssembler
{
    public static AuthenticatedUserResource ToResourceFromEntity(User entity, string token)
    {
        return new AuthenticatedUserResource(
            entity.Id,
            entity.Username.Value,
            entity.Email.Value,
            entity.Role.ToString(),
            entity.IsVerified,
            token);
    }
}