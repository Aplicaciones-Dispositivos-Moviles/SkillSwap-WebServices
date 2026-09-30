namespace SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;

/// <summary>
///     Lifecycle states of a certificate within the verification flow.
/// </summary>
public enum VerificationStatus
{
    Pending,
    Unverified,
    Suspicious,
    Verified,
    Rejected
}