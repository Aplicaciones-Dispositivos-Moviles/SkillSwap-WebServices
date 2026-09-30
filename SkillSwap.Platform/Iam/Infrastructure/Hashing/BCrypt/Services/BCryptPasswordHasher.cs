using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Domain.Services;
using BCryptNet = BCrypt.Net.BCrypt;

namespace SkillSwap.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;

/// <summary>
///     BCrypt implementation of <see cref="IPasswordHasher" />
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    /// <inheritdoc />
    public PasswordHash HashPassword(string plainPassword)
    {
        return new PasswordHash(BCryptNet.HashPassword(plainPassword));
    }

    /// <inheritdoc />
    public bool VerifyPassword(string plainPassword, PasswordHash hash)
    {
        return BCryptNet.Verify(plainPassword, hash.Value);
    }
}