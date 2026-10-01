using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Transform;

public static class ResolveVerificationCaseCommandFromResourceAssembler
{
    /// <remarks>
    ///     The verifier is always the authenticated user. A decision that is not Approved or Rejected becomes
    ///     an undefined value, which the service rejects as InvalidDecision.
    /// </remarks>
    public static ResolveVerificationCaseCommand ToCommandFromResource(int caseId, ResolveCaseResource resource,
        int verifierUserId)
    {
        return new ResolveVerificationCaseCommand(caseId, verifierUserId, ParseDecision(resource.Decision),
            resource.RubricNotes ?? string.Empty);
    }

    private static ReviewDecision ParseDecision(string? value)
    {
        return Enum.TryParse<ReviewDecision>(value, true, out var decision) && Enum.IsDefined(decision)
            ? decision
            : (ReviewDecision)(-1);
    }
}