using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Commands;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Transform;

public static class AttachCaseEvidenceCommandFromResourceAssembler
{
    public static AttachCaseEvidenceCommand ToCommandFromResource(int caseId, AttachEvidenceResource resource,
        int studentId)
    {
        return new AttachCaseEvidenceCommand(caseId, studentId, resource.EvidenceUrl ?? string.Empty);
    }
}