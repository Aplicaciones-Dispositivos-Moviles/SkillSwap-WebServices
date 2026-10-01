using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;
using SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;
using SkillSwap.Platform.CredentialVerification.Domain.Repositories;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.CredentialVerification.Integration;

public class CertificatePersistenceTests : ApiTestBase
{
    private static Certificate NewAssessed(int ownerId, string hash, string? number = null, string? code = null,
        int score = 0)
    {
        return new Certificate(ownerId, hash, $"certificates/{ownerId}/{hash}.jpg")
            .ApplyExtractedData("Ana Perez", "Coursera", "Backend", new DateOnly(2025, 3, 10), 40, number, code,
                "https://example.com/verify/1", "qr-payload", "full ocr text")
            .AssessRisk(new RiskAssessment(score));
    }

    private static async Task<Certificate> SaveAsync(Certificate certificate)
    {
        using var scope = TestApi.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ICertificateRepository>().AddAsync(certificate);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
        return certificate;
    }

    private static async Task<Certificate?> ReloadAsync(int id)
    {
        using var scope = TestApi.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ICertificateRepository>().FindByIdAsync(id);
    }

    [Fact]
    public async Task Certificate_RoundTripsEveryField()
    {
        var saved = await SaveAsync(NewAssessed(7, "hash-a", "cert-001", "code-xyz", score: 60));

        var loaded = (await ReloadAsync(saved.Id))!;

        Assert.Equal(7, loaded.OwnerId);
        Assert.Equal("Ana Perez", loaded.HolderName);
        Assert.Equal("Coursera", loaded.InstitutionName);
        Assert.Equal("Backend", loaded.CourseName);
        Assert.Equal(new DateOnly(2025, 3, 10), loaded.IssueDate);
        Assert.Equal(40, loaded.DurationHours);
        Assert.Equal("CERT-001", loaded.CertificateNumber);
        Assert.Equal("CODE-XYZ", loaded.VerificationCode);
        Assert.Equal("https://example.com/verify/1", loaded.VerificationUrl);
        Assert.Equal("qr-payload", loaded.QrPayload);
        Assert.Equal("full ocr text", loaded.OcrText);
        Assert.Equal("hash-a", loaded.FileHash);
        Assert.Equal("certificates/7/hash-a.jpg", loaded.StorageReference);
        Assert.Equal(VerificationStatus.Suspicious, loaded.Status);
        Assert.Equal(VerificationMethod.OcrOnly, loaded.VerificationMethod);
        Assert.Equal(60, loaded.RiskAssessment!.Score);
        Assert.Equal(RiskLevel.HighRisk, loaded.RiskAssessment.Level);
        Assert.Equal(DateTimeKind.Utc, loaded.CreatedAt.Kind);
        Assert.Null(loaded.VerifiedAt);
    }

    [Fact]
    public async Task PendingCertificate_WithoutRiskOrOcrData_RoundTripsWithNulls()
    {
        var saved = await SaveAsync(new Certificate(1, "hash-a", "certificates/1/hash-a.jpg"));

        var loaded = (await ReloadAsync(saved.Id))!;

        Assert.Equal(VerificationStatus.Pending, loaded.Status);
        Assert.Null(loaded.RiskAssessment);
        Assert.Null(loaded.HolderName);
        Assert.Null(loaded.IssueDate);
        Assert.Null(loaded.DurationHours);
        Assert.Null(loaded.CertificateNumber);
        Assert.Equal(string.Empty, loaded.OcrText);
    }

    [Fact]
    public async Task Status_IsStoredAsText()
    {
        await SaveAsync(NewAssessed(1, "hash-a", score: 60));

        using var scope = TestApi.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var statuses = await context.Database
            .SqlQueryRaw<string>("SELECT status AS \"Value\" FROM certificates").ToListAsync();

        Assert.Equal(["Suspicious"], statuses);
    }

    [Fact]
    public async Task Database_RejectsTheSameFileTwiceForTheSameOwner()
    {
        await SaveAsync(NewAssessed(1, "hash-a"));

        await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(NewAssessed(1, "hash-a")));
    }

    [Fact]
    public async Task Database_AllowsTheSameFileForDifferentOwners()
    {
        await SaveAsync(NewAssessed(1, "hash-a"));

        var second = await SaveAsync(NewAssessed(2, "hash-a"));

        Assert.True(second.Id > 0);
    }

    [Fact]
    public async Task ExistsQueries_DistinguishTheOwnerFromTheOthers()
    {
        await SaveAsync(NewAssessed(1, "hash-a", "cert-001", "code-xyz"));

        using var scope = TestApi.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICertificateRepository>();

        var own = await repository.FindByFileHashAsync(1, "hash-a", default);
        Assert.NotNull(own);
        Assert.Equal(1, own!.OwnerId);
        Assert.Null(await repository.FindByFileHashAsync(2, "hash-a", default));

        Assert.True(await repository.ExistsByFileHashExcludingOwnerAsync(2, "hash-a", default));
        Assert.False(await repository.ExistsByFileHashExcludingOwnerAsync(1, "hash-a", default));

        Assert.True(await repository.ExistsByCertificateNumberExcludingOwnerAsync(2, "CERT-001", default));
        Assert.False(await repository.ExistsByCertificateNumberExcludingOwnerAsync(1, "CERT-001", default));
        Assert.False(await repository.ExistsByCertificateNumberExcludingOwnerAsync(2, "CERT-999", default));

        Assert.True(await repository.ExistsByVerificationCodeExcludingOwnerAsync(2, "CODE-XYZ", default));
        Assert.False(await repository.ExistsByVerificationCodeExcludingOwnerAsync(1, "CODE-XYZ", default));
    }

    [Fact]
    public async Task FindByOwnerId_ReturnsOnlyThatOwnersCertificatesNewestFirst()
    {
        var first = await SaveAsync(NewAssessed(1, "hash-a"));
        await SaveAsync(NewAssessed(2, "hash-b"));
        var third = await SaveAsync(NewAssessed(1, "hash-c"));

        using var scope = TestApi.CreateScope();
        var found = (await scope.ServiceProvider.GetRequiredService<ICertificateRepository>()
            .FindByOwnerIdAsync(1, default)).ToList();

        Assert.Equal([third.Id, first.Id], found.Select(c => c.Id));
    }

    [Fact]
    public async Task ResolvedDispute_IsPersisted()
    {
        var saved = await SaveAsync(NewAssessed(1, "hash-a", score: 60));

        using (var scope = TestApi.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICertificateRepository>();
            var certificate = (await repository.FindByIdAsync(saved.Id))!;
            certificate.ResolveDispute(true);
            repository.Update(certificate);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CompleteAsync();
        }

        var loaded = (await ReloadAsync(saved.Id))!;
        Assert.Equal(VerificationStatus.Verified, loaded.Status);
        Assert.Equal(VerificationMethod.Manual, loaded.VerificationMethod);
        Assert.NotNull(loaded.VerifiedAt);
    }
}