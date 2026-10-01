using SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;
using SkillSwap.Platform.CredentialVerification.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.CredentialVerification.Interfaces.Rest.Transform;

public static class CertificateResourceFromEntityAssembler
{
    /// <remarks>
    ///     The file hash, the storage reference, the OCR text, the QR payload and the numeric risk score
    ///     are deliberately not exposed.
    /// </remarks>
    public static CertificateResource ToResourceFromEntity(Certificate entity, string fileUrl)
    {
        return new CertificateResource(
            entity.Id,
            entity.OwnerId,
            entity.HolderName,
            entity.InstitutionName,
            entity.CourseName,
            entity.IssueDate,
            entity.DurationHours,
            entity.CertificateNumber,
            entity.VerificationCode,
            entity.VerificationUrl,
            entity.Status.ToString(),
            entity.VerificationMethod.ToString(),
            entity.RiskAssessment?.Level.ToString(),
            entity.CreatedAt,
            entity.VerifiedAt,
            fileUrl);
    }
}