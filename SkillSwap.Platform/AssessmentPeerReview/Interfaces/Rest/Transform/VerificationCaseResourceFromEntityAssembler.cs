using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Transform;

public static class VerificationCaseResourceFromEntityAssembler
{
    public static VerificationCaseResource ToResourceFromEntity(VerificationCase entity)
    {
        return new VerificationCaseResource(
            entity.Id,
            entity.AttemptId,
            entity.StudentId,
            entity.VerifierUserId,
            entity.PathNodeId,
            entity.SkillTag,
            entity.Status.ToString(),
            entity.Decision?.ToString(),
            entity.RubricNotes,
            entity.EvidenceUrl,
            entity.OpenedAt,
            entity.AssignedAt,
            entity.ResolvedAt);
    }
}