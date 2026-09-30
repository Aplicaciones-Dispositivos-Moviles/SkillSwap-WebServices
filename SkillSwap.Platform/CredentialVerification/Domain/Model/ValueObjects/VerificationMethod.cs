namespace SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;

/// <summary>
///     Mechanisms contemplated to verify a certificate. Only <see cref="OcrOnly" /> and
///     <see cref="Manual" /> are executed in the implemented scope; the others are documented
///     as future extensions, with no active integration logic.
/// </summary>
public enum VerificationMethod
{
    OcrOnly,
    Qr,
    IssuerUrl,
    OfficialRegistry,
    Manual
}