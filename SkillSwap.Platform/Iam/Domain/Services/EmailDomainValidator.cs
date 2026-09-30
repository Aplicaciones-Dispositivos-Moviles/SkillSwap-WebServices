using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.Iam.Domain.Services;

/// <summary>
///     Domain service implementation: pure business rule with no external dependencies.
/// </summary>
public class EmailDomainValidator : IEmailDomainValidator
{
    public bool IsInstitutionalDomain(string email)
    {
        return Email.IsValid(email);
    }
}