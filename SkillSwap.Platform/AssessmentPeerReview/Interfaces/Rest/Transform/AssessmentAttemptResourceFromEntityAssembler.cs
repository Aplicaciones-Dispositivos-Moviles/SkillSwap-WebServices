using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Transform;

public static class AssessmentAttemptResourceFromEntityAssembler
{
    /// <remarks>
    ///     The answers the student chose are not part of the resource; the case, when known, is reported by
    ///     its id and status.
    /// </remarks>
    public static AssessmentAttemptResource ToResourceFromEntity(AssessmentAttempt entity,
        VerificationCase? verificationCase = null)
    {
        return new AssessmentAttemptResource(
            entity.Id,
            entity.BlueprintId,
            entity.StudentId,
            entity.Score.Value,
            entity.Score.Total,
            entity.Passed,
            entity.CompletedAt,
            verificationCase?.Id,
            verificationCase?.Status.ToString());
    }
}