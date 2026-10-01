using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Transform;

public static class UpdateVerifierAvailabilityCommandFromResourceAssembler
{
    /// <remarks>
    ///     The caller must have checked that the availability was sent: a missing value is never treated as false.
    /// </remarks>
    public static UpdateVerifierAvailabilityCommand ToCommandFromResource(VerifierAvailabilityResource resource,
        int userId)
    {
        return new UpdateVerifierAvailabilityCommand(userId, resource.Available!.Value);
    }
}