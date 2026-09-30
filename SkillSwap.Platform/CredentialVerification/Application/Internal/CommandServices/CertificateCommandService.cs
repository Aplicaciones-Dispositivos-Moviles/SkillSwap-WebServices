using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SkillSwap.Platform.CredentialVerification.Application.CommandServices;
using SkillSwap.Platform.CredentialVerification.Application.Internal.OutboundServices;
using SkillSwap.Platform.CredentialVerification.Domain.Model;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Commands;
using SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;
using SkillSwap.Platform.CredentialVerification.Domain.Repositories;
using SkillSwap.Platform.CredentialVerification.Domain.Services;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Shared.Resources.Errors;

namespace SkillSwap.Platform.CredentialVerification.Application.Internal.CommandServices;

/// <summary>
///     Certificate command service
/// </summary>
/// <param name="certificateRepository">Certificate repository</param>
/// <param name="riskScorer">Risk scorer domain service</param>
/// <param name="fileStorageService">File storage port</param>
/// <param name="unitOfWork">Unit of work</param>
/// <param name="localizer">String localizer for error messages</param>
/// <param name="logger">Logger</param>
public class CertificateCommandService(
    ICertificateRepository certificateRepository,
    ICertificateRiskScorer riskScorer,
    IFileStorageService fileStorageService,
    IUnitOfWork unitOfWork,
    IStringLocalizer<ErrorMessage> localizer,
    ILogger<CertificateCommandService> logger)
    : ICertificateCommandService
{
    public const int MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] PdfSignature = [0x25, 0x50, 0x44, 0x46, 0x2D]; // "%PDF-"

    /// <inheritdoc />
    public async Task<Result<Certificate>> Handle(UploadCertificateCommand command,
        CancellationToken cancellationToken)
    {
        var fileError = ValidateFile(command, out var contentType);
        if (fileError is not null) return Failure(fileError.Value);

        if (ExceedsFieldLimits(command)) return Failure(CredentialVerificationError.FieldTooLong);

        var fileHash = Convert.ToHexString(SHA256.HashData(command.FileContent)).ToLowerInvariant();
        if (await certificateRepository.ExistsByFileHashAsync(command.OwnerId, fileHash, cancellationToken))
            return Failure(CredentialVerificationError.DuplicateFile);

        string storageReference;
        try
        {
            storageReference = await fileStorageService.UploadAsync(
                command.FileContent, contentType!, $"certificates/{command.OwnerId}/{fileHash}", cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Failure(CredentialVerificationError.OperationCancelled);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not store the certificate file of user {OwnerId}", command.OwnerId);
            return Failure(CredentialVerificationError.StorageError);
        }

        try
        {
            var certificate = new Certificate(command.OwnerId, fileHash, storageReference)
                .ApplyExtractedData(
                    command.HolderName, command.InstitutionName, command.CourseName, command.IssueDate,
                    command.DurationHours, command.CertificateNumber, command.VerificationCode,
                    command.VerificationUrl, command.QrPayload, command.OcrText);

            // The aggregate normalizes the number and the code, so duplicates are looked up with its values.
            var duplicateNumber = certificate.CertificateNumber is not null
                                  && await certificateRepository.ExistsByCertificateNumberExcludingOwnerAsync(
                                      command.OwnerId, certificate.CertificateNumber, cancellationToken);
            var duplicateCode = certificate.VerificationCode is not null
                                && await certificateRepository.ExistsByVerificationCodeExcludingOwnerAsync(
                                    command.OwnerId, certificate.VerificationCode, cancellationToken);
            var duplicateFile = await certificateRepository.ExistsByFileHashExcludingOwnerAsync(
                command.OwnerId, fileHash, cancellationToken);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            certificate.AssessRisk(riskScorer.CalculateRisk(
                duplicateNumber, duplicateCode, duplicateFile, certificate.HasOcrInconsistencies(today)));

            await certificateRepository.AddAsync(certificate, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Certificate>.Success(certificate);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not register the certificate of user {OwnerId}", command.OwnerId);
            await DeleteQuietlyAsync(storageReference);
            return Failure(ToError(exception));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Certificate>> Handle(ResolveCertificateDisputeCommand command,
        CancellationToken cancellationToken)
    {
        var certificate = await certificateRepository.FindByIdAsync(command.CertificateId, cancellationToken);
        if (certificate is null) return Failure(CredentialVerificationError.CertificateNotFound);

        if (certificate.Status != VerificationStatus.Suspicious)
            return Failure(CredentialVerificationError.InvalidStatusTransition);

        try
        {
            certificate.ResolveDispute(command.IsAuthentic);
            certificateRepository.Update(certificate);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<Certificate>.Success(certificate);
        }
        catch (Exception exception)
        {
            return Failure(ToError(exception));
        }
    }

    /// <summary>
    ///     Checks size and type. The declared content type must match the real content of the file,
    ///     detected from its first bytes, so a renamed file is not accepted.
    /// </summary>
    private static CredentialVerificationError? ValidateFile(UploadCertificateCommand command,
        out string? contentType)
    {
        contentType = null;

        if (command.FileContent.Length == 0) return CredentialVerificationError.FileRequired;
        if (command.FileContent.Length > MaxFileSizeBytes) return CredentialVerificationError.FileTooLarge;

        var detected = DetectContentType(command.FileContent);
        var declared = NormalizeContentType(command.ContentType);
        if (detected is null || detected != declared) return CredentialVerificationError.InvalidFileType;

        contentType = detected;
        return null;
    }

    private static string? DetectContentType(byte[] content)
    {
        if (StartsWith(content, JpegSignature)) return "image/jpeg";
        if (StartsWith(content, PngSignature)) return "image/png";
        if (StartsWith(content, PdfSignature)) return "application/pdf";
        return null;
    }

    private static bool StartsWith(byte[] content, byte[] signature)
    {
        return content.Length >= signature.Length && content.AsSpan(0, signature.Length).SequenceEqual(signature);
    }

    private static string NormalizeContentType(string? contentType)
    {
        var normalized = (contentType ?? string.Empty).Trim().ToLowerInvariant();
        return normalized == "image/jpg" ? "image/jpeg" : normalized;
    }

    private static bool ExceedsFieldLimits(UploadCertificateCommand command)
    {
        return Exceeds(command.HolderName, Certificate.MaxTextLength)
               || Exceeds(command.InstitutionName, Certificate.MaxTextLength)
               || Exceeds(command.CourseName, Certificate.MaxTextLength)
               || Exceeds(command.CertificateNumber, Certificate.MaxTextLength)
               || Exceeds(command.VerificationCode, Certificate.MaxTextLength)
               || Exceeds(command.VerificationUrl, Certificate.MaxUrlLength)
               || Exceeds(command.QrPayload, Certificate.MaxQrPayloadLength)
               || Exceeds(command.OcrText, Certificate.MaxOcrTextLength);
    }

    private static bool Exceeds(string? value, int maxLength)
    {
        return value is not null && value.Trim().Length > maxLength;
    }

    private static CredentialVerificationError ToError(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => CredentialVerificationError.OperationCancelled,
            DbUpdateException => CredentialVerificationError.DatabaseError,
            _ => CredentialVerificationError.InternalServerError
        };
    }

    /// <summary>
    ///     Compensates an upload whose certificate could not be registered. It uses no cancellation
    ///     token on purpose: the request may already have been cancelled.
    /// </summary>
    private async Task DeleteQuietlyAsync(string storageReference)
    {
        try
        {
            await fileStorageService.DeleteAsync(storageReference, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Orphaned certificate file could not be deleted: {StorageReference}",
                storageReference);
        }
    }

    private Result<Certificate> Failure(CredentialVerificationError error)
    {
        return Result<Certificate>.Failure(error, localizer[error.ToString()]);
    }
}