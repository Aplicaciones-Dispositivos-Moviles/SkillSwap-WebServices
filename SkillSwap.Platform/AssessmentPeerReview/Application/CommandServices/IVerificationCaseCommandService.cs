using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;
using SkillSwap.Platform.Shared.Application.Model;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.CommandServices;

public interface IVerificationCaseCommandService
{
    Task<Result<VerificationCase>> Handle(AttachCaseEvidenceCommand command, CancellationToken cancellationToken);

    Task<Result<VerificationCase>> Handle(ResolveVerificationCaseCommand command,
        CancellationToken cancellationToken);
}