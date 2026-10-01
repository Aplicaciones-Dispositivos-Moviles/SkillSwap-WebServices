using SkillSwap.Platform.AssessmentPeerReview.Application.QueryServices;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Transform;

public static class VerificationCaseDetailResourceFromDetailAssembler
{
    public static VerificationCaseDetailResource ToResourceFromDetail(VerificationCaseDetail detail)
    {
        return new VerificationCaseDetailResource(
            VerificationCaseResourceFromEntityAssembler.ToResourceFromEntity(detail.Case),
            AssessmentAttemptResourceFromEntityAssembler.ToResourceFromEntity(detail.Attempt, detail.Case),
            detail.FailedQuestions
                .Select(q => new FailedQuestionResource(q.Position, q.Text, q.Answers, q.SelectedAnswer))
                .ToList());
    }
}