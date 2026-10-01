using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Transform;

public static class SubmitAssessmentAttemptCommandFromResourceAssembler
{
    /// <remarks>
    ///     The student is always the authenticated user, never a value from the request body.
    /// </remarks>
    public static SubmitAssessmentAttemptCommand ToCommandFromResource(SubmitAssessmentAttemptResource resource,
        int studentId)
    {
        return new SubmitAssessmentAttemptCommand(studentId, resource.BlueprintId, resource.SelectedAnswers ?? []);
    }
}