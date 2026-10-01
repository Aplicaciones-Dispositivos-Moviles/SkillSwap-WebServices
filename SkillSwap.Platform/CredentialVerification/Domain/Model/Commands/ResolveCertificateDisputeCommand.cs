namespace SkillSwap.Platform.CredentialVerification.Domain.Model.Commands;

/// <summary>
///     Resolve certificate dispute command
/// </summary>
/// <param name="CertificateId">The escalated certificate</param>
/// <param name="IsAuthentic">The Coordinator's decision: true to verify, false to reject</param>
public record ResolveCertificateDisputeCommand(int CertificateId, bool IsAuthentic);