using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Transform;

public static class VerifierProfileResourceFromEntityAssembler
{
    public static VerifierProfileResource ToResourceFromEntity(VerifierProfile entity)
    {
        return new VerifierProfileResource(
            entity.Id,
            entity.VerifierUserId,
            entity.SkillTags,
            entity.Available,
            entity.Verified,
            entity.Rating,
            entity.ReviewCount,
            entity.CreatedAt);
    }
}