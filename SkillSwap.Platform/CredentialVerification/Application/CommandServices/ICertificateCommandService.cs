using SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Commands;
using SkillSwap.Platform.Shared.Application.Model;

namespace SkillSwap.Platform.CredentialVerification.Application.CommandServices;

/// <summary>
///     Certificate command service interface
/// </summary>
public interface ICertificateCommandService
{
    /// <summary>
    ///     Handle upload certificate command: validates the file, stores it, evaluates the risk
    ///     and registers the certificate.
    /// </summary>
    /// <returns>The <see cref="Result{T}" /> wrapping the registered certificate</returns>
    Task<Result<Certificate>> Handle(UploadCertificateCommand command, CancellationToken cancellationToken);

    /// <summary>
    ///     Handle resolve certificate dispute command.
    /// </summary>
    /// <returns>The <see cref="Result{T}" /> wrapping the resolved certificate</returns>
    Task<Result<Certificate>> Handle(ResolveCertificateDisputeCommand command, CancellationToken cancellationToken);
}