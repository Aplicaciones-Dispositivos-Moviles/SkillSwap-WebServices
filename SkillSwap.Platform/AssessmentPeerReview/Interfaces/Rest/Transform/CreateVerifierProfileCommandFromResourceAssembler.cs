using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Transform;

public static class CreateVerifierProfileCommandFromResourceAssembler
{
    public static CreateVerifierProfileCommand ToCommandFromResource(CreateVerifierProfileResource resource,
        int userId)
    {
        return new CreateVerifierProfileCommand(userId, resource.SkillTag ?? string.Empty);
    }
}