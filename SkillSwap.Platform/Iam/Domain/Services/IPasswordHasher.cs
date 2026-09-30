using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.Iam.Domain.Services;

/// <summary>
///     Contract for hashing and verifying passwords, decoupling the domain from the
///     concrete cryptographic algorithm.
/// </summary>
public interface IPasswordHasher
{
    PasswordHash HashPassword(string plainPassword);
    bool VerifyPassword(string plainPassword, PasswordHash hash);
}