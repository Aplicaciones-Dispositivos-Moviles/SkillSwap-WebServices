using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;
using SkillSwap.Platform.Shared.Application.Model;

namespace SkillSwap.Platform.AssessmentPeerReview.Application.CommandServices;

public interface IVerifierProfileCommandService
{
    Task<Result<VerifierProfile>> Handle(CreateVerifierProfileCommand command, CancellationToken cancellationToken);

    Task<Result<VerifierProfile>> Handle(UpdateVerifierAvailabilityCommand command,
        CancellationToken cancellationToken);
}