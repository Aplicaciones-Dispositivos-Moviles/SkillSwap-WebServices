using SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.CredentialVerification.Domain.Services;

/// <summary>
///     Contract for calculating the risk of a certificate from an explainable set of rules,
///     without depending on external verification sources.
/// </summary>
public interface ICertificateRiskScorer
{
    RiskAssessment CalculateRisk(
        bool duplicateCertificateNumber,
        bool duplicateVerificationCode,
        bool duplicateFileHash,
        bool ocrInconsistencies);
}