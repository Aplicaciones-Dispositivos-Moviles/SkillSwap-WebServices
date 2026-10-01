using SkillSwap.Platform.CredentialVerification.Application.Internal.QueryServices;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Queries;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.CredentialVerification.Application;

public class CertificateQueryServiceTests
{
    private readonly FakeCertificateRepository _repository = new();
    private readonly CertificateQueryService _service;

    public CertificateQueryServiceTests()
    {
        _service = new CertificateQueryService(_repository, new FakeFileStorageService());
    }

    private async Task<Certificate> AddAsync(int ownerId, string hash)
    {
        var certificate = new Certificate(ownerId, hash, $"stored/{hash}");
        await _repository.AddAsync(certificate);
        return certificate;
    }

    [Fact]
    public async Task GetById_ReturnsTheCertificate()
    {
        var certificate = await AddAsync(1, "hash-a");

        var found = await _service.Handle(new GetCertificateByIdQuery(certificate.Id), CancellationToken.None);

        Assert.Same(certificate, found);
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNull()
    {
        Assert.Null(await _service.Handle(new GetCertificateByIdQuery(99), CancellationToken.None));
    }

    [Fact]
    public async Task GetByOwnerId_ReturnsOnlyTheCertificatesOfThatOwner()
    {
        await AddAsync(1, "hash-a");
        await AddAsync(1, "hash-b");
        await AddAsync(2, "hash-c");

        var found = (await _service.Handle(new GetCertificatesByOwnerIdQuery(1), CancellationToken.None)).ToList();

        Assert.Equal(2, found.Count);
        Assert.All(found, c => Assert.Equal(1, c.OwnerId));
    }

    [Fact]
    public async Task GetFileUrl_ReturnsATemporaryUrlValidForFifteenMinutes()
    {
        var certificate = await AddAsync(1, "hash-a");

        var url = _service.GetFileUrl(certificate);

        Assert.Equal("https://files.test/stored/hash-a?minutes=15", url);
    }
}