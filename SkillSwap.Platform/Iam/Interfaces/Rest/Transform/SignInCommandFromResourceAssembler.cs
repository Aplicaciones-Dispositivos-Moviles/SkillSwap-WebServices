using SkillSwap.Platform.Iam.Domain.Model.Commands;
using SkillSwap.Platform.Iam.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.Iam.Interfaces.Rest.Transform;

public static class SignInCommandFromResourceAssembler
{
    public static SignInCommand ToCommandFromResource(SignInResource resource)
    {
        return new SignInCommand(resource.Username.Trim(), resource.Password);
    }
}