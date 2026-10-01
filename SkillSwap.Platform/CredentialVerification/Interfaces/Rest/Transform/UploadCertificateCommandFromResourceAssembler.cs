using SkillSwap.Platform.CredentialVerification.Domain.Model.Commands;
using SkillSwap.Platform.CredentialVerification.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.CredentialVerification.Interfaces.Rest.Transform;

public static class UploadCertificateCommandFromResourceAssembler
{
    /// <remarks>
    ///     A missing or empty file becomes an empty content, which the application layer rejects as
    ///     FileRequired. The owner comes from the authenticated user, never from the form.
    /// </remarks>
    public static async Task<UploadCertificateCommand> ToCommandFromResourceAsync(
        UploadCertificateResource resource, int ownerId, CancellationToken cancellationToken)
    {
        byte[] content = [];
        if (resource.File is { Length: > 0 } file)
        {
            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream((int)file.Length);
            await stream.CopyToAsync(buffer, cancellationToken);
            content = buffer.ToArray();
        }

        return new UploadCertificateCommand(
            ownerId,
            resource.File?.ContentType ?? string.Empty,
            content,
            resource.HolderName,
            resource.InstitutionName,
            resource.CourseName,
            resource.IssueDate,
            resource.DurationHours,
            resource.CertificateNumber,
            resource.VerificationCode,
            resource.VerificationUrl,
            resource.QrPayload,
            resource.OcrText);
    }
}