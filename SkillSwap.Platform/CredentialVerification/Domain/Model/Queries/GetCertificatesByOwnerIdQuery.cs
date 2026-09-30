namespace SkillSwap.Platform.CredentialVerification.Domain.Model.Queries;

/// <summary>
///     Get the certificates uploaded by a student
/// </summary>
/// <param name="OwnerId">The student who owns the certificates</param>
public record GetCertificatesByOwnerIdQuery(int OwnerId);