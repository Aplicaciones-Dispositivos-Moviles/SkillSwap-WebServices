using SkillSwap.Platform.CredentialVerification.Application.ACL;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;
using SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.CredentialVerification.Application;

public class CredentialContextFacadeTests
{
    private readonly CredentialContextFacade _facade;
    private readonly FakeCertificateRepository _repository = new();

    public CredentialContextFacadeTests()
    {
        _facade = new CredentialContextFacade(_repository);
    }

    /// <summary>
    ///     Adds a certificate. Risk 0 leaves it Unverified, 60 makes it Suspicious, and resolving a
    ///     suspicious one makes it Verified (true) or Rejected (false).
    /// </summary>
    private async Task<Certificate> AddAsync(int ownerId, string hash, int riskScore, bool? resolvedAs = null,
        string? course = "Course", string? institution = "Coursera")
    {
        var certificate = new Certificate(ownerId, hash, $"ref/{hash}")
            .ApplyExtractedData(null, institution, course, null, null, null, null, null, null, null)
            .AssessRisk(new RiskAssessment(riskScore));
        if (resolvedAs is not null) certificate.ResolveDispute(resolvedAs.Value);
        await _repository.AddAsync(certificate);
        return certificate;
    }

    [Fact]
    public async Task GetEvidenceCertificates_ReturnsOnlyUnverifiedAndVerifiedOnes()
    {
        var unverified = await AddAsync(1, "h1", riskScore: 0);
        await AddAsync(1, "h2", riskScore: 60);
        await AddAsync(1, "h3", riskScore: 60, resolvedAs: false);
        var verified = await AddAsync(1, "h4", riskScore: 60, resolvedAs: true);

        var found = await _facade.GetEvidenceCertificatesAsync(1, CancellationToken.None);

        Assert.Equal([unverified.Id, verified.Id], found.Select(c => c.Id));
    }

    [Fact]
    public async Task GetEvidenceCertificates_ReturnsOnlyTheOwnersCertificatesOldestFirst()
    {
        var first = await AddAsync(1, "h1", riskScore: 0);
        await AddAsync(2, "h2", riskScore: 0);
        var third = await AddAsync(1, "h3", riskScore: 0);

        var found = await _facade.GetEvidenceCertificatesAsync(1, CancellationToken.None);

        Assert.Equal([first.Id, third.Id], found.Select(c => c.Id));
    }

    [Fact]
    public async Task GetEvidenceCertificates_ExposesOnlyTheSummary()
    {
        await AddAsync(1, "h1", riskScore: 0, course: "Backend with ASP.NET", institution: "Coursera");
        await AddAsync(1, "h2", riskScore: 0, course: null, institution: null);

        var found = await _facade.GetEvidenceCertificatesAsync(1, CancellationToken.None);

        Assert.Equal("Backend with ASP.NET", found[0].CourseName);
        Assert.Equal("Coursera", found[0].InstitutionName);
        Assert.Null(found[1].CourseName);
    }

    [Fact]
    public async Task GetEvidenceCertificates_ForAStudentWithoutCertificates_ReturnsAnEmptyList()
    {
        Assert.Empty(await _facade.GetEvidenceCertificatesAsync(1, CancellationToken.None));
    }
}