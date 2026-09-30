namespace SkillSwap.Platform.Iam.Domain.Services;

/// <summary>
///     Contract for checking that an email belongs to an authorized institutional domain
///     before an account is created.
/// </summary>
public interface IEmailDomainValidator
{
    bool IsInstitutionalDomain(string email);
}