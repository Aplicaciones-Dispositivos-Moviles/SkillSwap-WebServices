using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.Iam.Interfaces.Rest.Transform;

public static class UserResourceFromEntityAssembler
{
    public static UserResource ToResourceFromEntity(User entity)
    {
        return new UserResource(
            entity.Id,
            entity.Username.Value,
            entity.Email.Value,
            entity.Role.ToString(),
            entity.IsVerified,
            entity.Bio);
    }
}