using SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;
using SkillSwap.Platform.CredentialVerification.Domain.Repositories;

namespace SkillSwap.Platform.Tests.Support;

public class FakeCertificateRepository : ICertificateRepository
{
    private readonly List<Certificate> _certificates = [];
    private int _nextId = 1;

    public IReadOnlyList<Certificate> Certificates => _certificates;

    public Task AddAsync(Certificate entity, CancellationToken cancellationToken = default)
    {
        typeof(Certificate).GetProperty(nameof(Certificate.Id))!.SetValue(entity, _nextId++);
        _certificates.Add(entity);
        return Task.CompletedTask;
    }

    public Task<Certificate?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_certificates.FirstOrDefault(c => c.Id == id));
    }

    public void Update(Certificate entity)
    {
    }

    public void Remove(Certificate entity)
    {
        _certificates.Remove(entity);
    }

    public Task<IEnumerable<Certificate>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Certificate>>(_certificates.ToList());
    }

    public Task<IEnumerable<Certificate>> FindByOwnerIdAsync(int ownerId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IEnumerable<Certificate>>(_certificates.Where(c => c.OwnerId == ownerId).ToList());
    }

    public Task<Certificate?> FindByFileHashAsync(int ownerId, string fileHash, CancellationToken cancellationToken)
    {
        return Task.FromResult(_certificates.FirstOrDefault(c => c.OwnerId == ownerId && c.FileHash == fileHash));
    }

    public Task<bool> ExistsByCertificateNumberExcludingOwnerAsync(int ownerId, string certificateNumber,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            _certificates.Any(c => c.OwnerId != ownerId && c.CertificateNumber == certificateNumber));
    }

    public Task<bool> ExistsByVerificationCodeExcludingOwnerAsync(int ownerId, string verificationCode,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            _certificates.Any(c => c.OwnerId != ownerId && c.VerificationCode == verificationCode));
    }

    public Task<bool> ExistsByFileHashExcludingOwnerAsync(int ownerId, string fileHash,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_certificates.Any(c => c.OwnerId != ownerId && c.FileHash == fileHash));
    }
}