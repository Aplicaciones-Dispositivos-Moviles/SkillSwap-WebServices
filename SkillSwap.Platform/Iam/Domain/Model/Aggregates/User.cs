using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Iam.Domain.Model.Aggregates;

/// <summary>
///     User aggregate root
/// </summary>
/// <remarks>
///     Centralizes the account information of a registered SkillSwap user. Student accounts can
///     also become Verifiers (see the Assessment &amp; Peer Review bounded context);
///     Coordinator is a separate role.
/// </remarks>
public class User
{
    public const int MaxBioLength = 1000;

    /// <summary>
    ///     Parameterless constructor required by EF Core.
    /// </summary>
    protected User()
    {
        Username = null!;
        Email = null!;
        PasswordHash = null!;
    }

    public User(Username username, Email email, PasswordHash passwordHash, UserRole role)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    public int Id { get; private set; }
    public Username Username { get; private set; }
    public Email Email { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsVerified { get; private set; }
    public string Bio { get; private set; } = string.Empty;
    public DeviceToken? DeviceToken { get; private set; }

    /// <summary>
    ///     Mark the account as verified once the institutional validation is confirmed.
    /// </summary>
    public User Verify()
    {
        IsVerified = true;
        return this;
    }

    /// <summary>
    ///     Update the free-text profile description.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the bio exceeds <see cref="MaxBioLength" />.</exception>
    public User UpdateBio(string bio)
    {
        var trimmed = bio.Trim();
        if (trimmed.Length > MaxBioLength)
            throw new DomainException($"The bio cannot exceed {MaxBioLength} characters.");
        Bio = trimmed;
        return this;
    }

    /// <summary>
    ///     Associate or update the device token of the mobile device the user signed in from.
    /// </summary>
    public User RegisterDeviceToken(string token)
    {
        DeviceToken = new DeviceToken(token);
        return this;
    }
}