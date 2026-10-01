using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;
using SkillSwap.Platform.Shared.Application.Model;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.CommandServices;

public interface IAssessmentAttemptCommandService
{
    Task<Result<SubmitAssessmentAttemptOutcome>> Handle(SubmitAssessmentAttemptCommand command,
        CancellationToken cancellationToken);
}