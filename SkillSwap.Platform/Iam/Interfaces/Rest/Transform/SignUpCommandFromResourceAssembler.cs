using SkillSwap.Platform.Iam.Domain.Model.Commands;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.Iam.Interfaces.Rest.Transform;

public static class SignUpCommandFromResourceAssembler
{
    /// <remarks>
    ///     The password is passed through untouched: trimming it would change what the user typed.
    /// </remarks>
    public static SignUpCommand ToCommandFromResource(SignUpResource resource, UserRole role)
    {
        return new SignUpCommand(resource.Username.Trim(), resource.Email.Trim(), resource.Password, role);
    }
}