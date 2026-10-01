using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SkillSwap.Platform.CredentialVerification.Application.Internal.CommandServices;
using SkillSwap.Platform.CredentialVerification.Domain.Model;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Commands;
using SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;
using SkillSwap.Platform.CredentialVerification.Domain.Services;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Resources.Errors;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.CredentialVerification.Application;

public class CertificateCommandServiceTests
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
    private static readonly byte[] OtherJpeg = [0xFF, 0xD8, 0xFF, 0xE1, 0x01, 0x02, 0x03];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
    private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];

    private readonly FakeCertificateRepository _repository = new();
    private readonly CertificateCommandService _service;
    private readonly FakeFileStorageService _storage = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    public CertificateCommandServiceTests()
    {
        _service = new CertificateCommandService(
            _repository,
            new CertificateRiskScorer(),
            _storage,
            _unitOfWork,
            new FakeLocalizer<ErrorMessage>(),
            NullLogger<CertificateCommandService>.Instance);
    }

    private static string Hash(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }

    private static UploadCertificateCommand Upload(int ownerId = 1, byte[]? file = null,
        string contentType = "image/jpeg", string? holder = "Ana Perez", string? number = "cert-001",
        string? code = "code-xyz")
    {
        return new UploadCertificateCommand(ownerId, contentType, file ?? Jpeg, holder, "Coursera", "Backend",
            new DateOnly(2025, 3, 10), 40, number, code, null, null, "ocr text");
    }

    private async Task SeedCertificateAsync(int ownerId, byte[] file, string? number = null, string? code = null)
    {
        var certificate = new Certificate(ownerId, Hash(file), "seeded/ref")
            .ApplyExtractedData(null, null, null, null, null, number, code, null, null, null);
        await _repository.AddAsync(certificate);
    }

    private static void AssertFailure(Result<Certificate> result, CredentialVerificationError expected)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expected, Assert.IsType<CredentialVerificationError>(result.Error));
    }

    // ---------- Upload: happy paths ----------

    [Fact]
    public async Task Upload_WithValidFileAndCleanData_RegistersAnUnverifiedLowRiskCertificate()
    {
        var result = await _service.Handle(Upload(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var certificate = Assert.Single(_repository.Certificates);
        Assert.Equal(VerificationStatus.Unverified, certificate.Status);
        Assert.Equal(0, certificate.RiskAssessment!.Score);
        Assert.Equal(RiskLevel.LowRisk, certificate.RiskAssessment.Level);
        Assert.Equal(1, certificate.OwnerId);
        Assert.Equal("CERT-001", certificate.CertificateNumber);
        Assert.Equal(1, _unitOfWork.CompleteCalls);
    }

    [Theory]
    [MemberData(nameof(AcceptedFiles))]
    public async Task Upload_AcceptsJpegPngAndPdf(byte[] file, string contentType, string expectedStoredType)
    {
        var result = await _service.Handle(Upload(file: file, contentType: contentType), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedStoredType, Assert.Single(_storage.Uploads).ContentType);
    }

    public static IEnumerable<object[]> AcceptedFiles()
    {
        yield return [Jpeg, "image/jpeg", "image/jpeg"];
        yield return [Jpeg, "image/jpg", "image/jpeg"];
        yield return [Png, "image/png", "image/png"];
        yield return [Pdf, "application/pdf", "application/pdf"];
        yield return [Pdf, "Application/PDF", "application/pdf"];
    }

    [Fact]
    public async Task Upload_StoresTheFileUnderTheOwnerAndTheServerSideHash()
    {
        var result = await _service.Handle(Upload(ownerId: 7), CancellationToken.None);

        var expectedHash = Hash(Jpeg);
        Assert.Equal(expectedHash, result.Value!.FileHash);
        var upload = Assert.Single(_storage.Uploads);
        Assert.Equal($"certificates/7/{expectedHash}", upload.Key);
        Assert.Equal($"stored/certificates/7/{expectedHash}", result.Value.StorageReference);
    }

    [Fact]
    public async Task Upload_WithoutAnyOcrData_SucceedsAndCountsTheInconsistency()
    {
        var command = new UploadCertificateCommand(1, "image/jpeg", Jpeg, null, null, null, null, null, null, null,
            null, null, null);

        var result = await _service.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value!.RiskAssessment!.Score);
        Assert.Equal(VerificationStatus.Unverified, result.Value.Status);
        Assert.Null(result.Value.HolderName);
    }

    // ---------- Upload: risk ----------

    [Fact]
    public async Task Upload_WhenAnotherUserRegisteredTheSameNumber_ScoresReviewAndStaysUnverified()
    {
        await SeedCertificateAsync(2, OtherJpeg, "CERT-001");

        var result = await _service.Handle(Upload(ownerId: 1), CancellationToken.None);

        Assert.Equal(30, result.Value!.RiskAssessment!.Score);
        Assert.Equal(RiskLevel.Review, result.Value.RiskAssessment.Level);
        Assert.Equal(VerificationStatus.Unverified, result.Value.Status);
    }

    [Fact]
    public async Task Upload_WhenAnotherUserRegisteredTheSameNumberAndCode_BecomesSuspicious()
    {
        await SeedCertificateAsync(2, OtherJpeg, "CERT-001", "CODE-XYZ");

        var result = await _service.Handle(Upload(ownerId: 1), CancellationToken.None);

        Assert.Equal(60, result.Value!.RiskAssessment!.Score);
        Assert.Equal(VerificationStatus.Suspicious, result.Value.Status);
    }

    [Fact]
    public async Task Upload_WhenAnotherUserUploadedTheSameFile_BecomesSuspicious()
    {
        await SeedCertificateAsync(2, Jpeg);

        var result = await _service.Handle(Upload(ownerId: 1), CancellationToken.None);

        Assert.Equal(50, result.Value!.RiskAssessment!.Score);
        Assert.Equal(RiskLevel.HighRisk, result.Value.RiskAssessment.Level);
        Assert.Equal(VerificationStatus.Suspicious, result.Value.Status);
    }

    [Fact]
    public async Task Upload_WhenTheSameOwnerRegisteredTheSameNumber_IsNotADuplicate()
    {
        await SeedCertificateAsync(1, OtherJpeg, "CERT-001", "CODE-XYZ");

        var result = await _service.Handle(Upload(ownerId: 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.RiskAssessment!.Score);
    }

    [Fact]
    public async Task Upload_DetectsDuplicatesRegardlessOfCapitalization()
    {
        await SeedCertificateAsync(2, OtherJpeg, "CERT-001");

        var result = await _service.Handle(Upload(ownerId: 1, number: "  Cert-001 "), CancellationToken.None);

        Assert.Equal(30, result.Value!.RiskAssessment!.Score);
    }

    // ---------- Upload: rejections ----------

    [Fact]
    public async Task Upload_OfTheSameFileByTheSameOwner_ReturnsDuplicateFileReferencingTheExistingCertificate()
    {
        var first = await _service.Handle(Upload(), CancellationToken.None);

        var result = await _service.Handle(Upload(), CancellationToken.None);

        AssertFailure(result, CredentialVerificationError.DuplicateFile);
        Assert.Equal(first.Value!.Id, result.Details!["existingCertificateId"]);
        Assert.Single(_storage.Uploads);
        Assert.Single(_repository.Certificates);
    }

    [Fact]
    public async Task Upload_WithAnEmptyFile_ReturnsFileRequired()
    {
        var result = await _service.Handle(Upload(file: []), CancellationToken.None);

        AssertFailure(result, CredentialVerificationError.FileRequired);
        Assert.Empty(_storage.Uploads);
    }

    [Fact]
    public async Task Upload_WithAFileOverTenMegabytes_ReturnsFileTooLarge()
    {
        var big = new byte[CertificateCommandService.MaxFileSizeBytes + 1];
        big[0] = 0xFF;
        big[1] = 0xD8;
        big[2] = 0xFF;

        var result = await _service.Handle(Upload(file: big), CancellationToken.None);

        AssertFailure(result, CredentialVerificationError.FileTooLarge);
        Assert.Empty(_storage.Uploads);
    }

    [Fact]
    public async Task Upload_WithAFileOfExactlyTenMegabytes_IsAccepted()
    {
        var exact = new byte[CertificateCommandService.MaxFileSizeBytes];
        exact[0] = 0xFF;
        exact[1] = 0xD8;
        exact[2] = 0xFF;

        var result = await _service.Handle(Upload(file: exact), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Upload_WithATextFile_ReturnsInvalidFileType()
    {
        var text = "hello world"u8.ToArray();

        var result = await _service.Handle(Upload(file: text, contentType: "text/plain"), CancellationToken.None);

        AssertFailure(result, CredentialVerificationError.InvalidFileType);
    }

    [Fact]
    public async Task Upload_WhenTheDeclaredTypeDoesNotMatchTheContent_ReturnsInvalidFileType()
    {
        var result = await _service.Handle(Upload(file: Jpeg, contentType: "image/png"), CancellationToken.None);

        AssertFailure(result, CredentialVerificationError.InvalidFileType);
        Assert.Empty(_storage.Uploads);
    }

    [Fact]
    public async Task Upload_WhenARenamedFileDeclaresAnAllowedType_ReturnsInvalidFileType()
    {
        var script = "MZ-not-an-image"u8.ToArray();

        var result = await _service.Handle(Upload(file: script, contentType: "image/jpeg"), CancellationToken.None);

        AssertFailure(result, CredentialVerificationError.InvalidFileType);
    }

    [Fact]
    public async Task Upload_WithAFieldOverItsMaximumLength_ReturnsFieldTooLongWithoutUploading()
    {
        var result = await _service.Handle(Upload(holder: new string('a', Certificate.MaxTextLength + 1)),
            CancellationToken.None);

        AssertFailure(result, CredentialVerificationError.FieldTooLong);
        Assert.Empty(_storage.Uploads);
    }

    // ---------- Upload: infrastructure failures ----------

    [Fact]
    public async Task Upload_WhenStorageFails_ReturnsStorageErrorAndSavesNothing()
    {
        _storage.UploadException = new InvalidOperationException("storage down");

        var result = await _service.Handle(Upload(), CancellationToken.None);

        AssertFailure(result, CredentialVerificationError.StorageError);
        Assert.Empty(_repository.Certificates);
        Assert.Equal(0, _unitOfWork.CompleteCalls);
    }

    [Fact]
    public async Task Upload_WhenSavingFails_DeletesTheStoredFileAndReturnsDatabaseError()
    {
        _unitOfWork.ExceptionToThrow = new DbUpdateException("failure");

        var result = await _service.Handle(Upload(), CancellationToken.None);

        AssertFailure(result, CredentialVerificationError.DatabaseError);
        var upload = Assert.Single(_storage.Uploads);
        Assert.Equal($"stored/{upload.Key}", Assert.Single(_storage.Deleted));
    }

    [Fact]
    public async Task Upload_WhenSavingIsCancelled_DeletesTheStoredFileAndReturnsOperationCancelled()
    {
        _unitOfWork.ExceptionToThrow = new OperationCanceledException();

        var result = await _service.Handle(Upload(), CancellationToken.None);

        AssertFailure(result, CredentialVerificationError.OperationCancelled);
        Assert.Single(_storage.Deleted);
    }

    // ---------- Resolve dispute ----------

    private async Task<Certificate> RegisterSuspiciousAsync()
    {
        await SeedCertificateAsync(2, OtherJpeg, "CERT-001", "CODE-XYZ");
        var result = await _service.Handle(Upload(ownerId: 1), CancellationToken.None);
        return result.Value!;
    }

    [Theory]
    [InlineData(true, VerificationStatus.Verified)]
    [InlineData(false, VerificationStatus.Rejected)]
    public async Task ResolveDispute_OnASuspiciousCertificate_SetsTheFinalStatus(bool isAuthentic,
        VerificationStatus expected)
    {
        var suspicious = await RegisterSuspiciousAsync();

        var result = await _service.Handle(new ResolveCertificateDisputeCommand(suspicious.Id, isAuthentic),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value!.Status);
        Assert.Equal(VerificationMethod.Manual, result.Value.VerificationMethod);
    }

    [Fact]
    public async Task ResolveDispute_ForAnUnknownCertificate_ReturnsCertificateNotFound()
    {
        var result = await _service.Handle(new ResolveCertificateDisputeCommand(99, true), CancellationToken.None);

        AssertFailure(result, CredentialVerificationError.CertificateNotFound);
    }

    [Fact]
    public async Task ResolveDispute_ForANonSuspiciousCertificate_ReturnsInvalidStatusTransition()
    {
        var clean = (await _service.Handle(Upload(), CancellationToken.None)).Value!;

        var result = await _service.Handle(new ResolveCertificateDisputeCommand(clean.Id, true),
            CancellationToken.None);

        AssertFailure(result, CredentialVerificationError.InvalidStatusTransition);
    }

    [Fact]
    public async Task ResolveDispute_Twice_ReturnsInvalidStatusTransition()
    {
        var suspicious = await RegisterSuspiciousAsync();
        await _service.Handle(new ResolveCertificateDisputeCommand(suspicious.Id, true), CancellationToken.None);

        var result = await _service.Handle(new ResolveCertificateDisputeCommand(suspicious.Id, false),
            CancellationToken.None);

        AssertFailure(result, CredentialVerificationError.InvalidStatusTransition);
    }
}