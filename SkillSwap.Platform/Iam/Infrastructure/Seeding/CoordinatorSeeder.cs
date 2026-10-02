using System.Text;
using Microsoft.Extensions.Options;
using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Domain.Repositories;
using SkillSwap.Platform.Iam.Domain.Services;
using SkillSwap.Platform.Shared.Domain.Repositories;

namespace SkillSwap.Platform.Iam.Infrastructure.Seeding;

public enum CoordinatorSeedOutcome
{
    NotConfigured,
    Invalid,
    AlreadyExists,
    Created
}

/// <summary>
///     Creates the Coordinator account described by <see cref="CoordinatorSeedSettings" /> when it does not
///     exist yet. A missing or malformed configuration is reported in the log and never stops the API.
/// </summary>
/// <param name="userRepository">User repository</param>
/// <param name="passwordHasher">Password hasher</param>
/// <param name="unitOfWork">Unit of work</param>
/// <param name="options">The configured account</param>
/// <param name="logger">Logger</param>
public class CoordinatorSeeder(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IOptions<CoordinatorSeedSettings> options,
    ILogger<CoordinatorSeeder> logger)
{
    // The same rule as the sign-up: at least 8 characters, and BCrypt ignores anything beyond 72 bytes.
    private const int MinPasswordLength = 8;
    private const int MaxPasswordBytes = 72;

    public async Task<CoordinatorSeedOutcome> SeedAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var provided = new[] { settings.Username, settings.Email, settings.Password }
            .Count(value => !string.IsNullOrWhiteSpace(value));

        if (provided == 0) return CoordinatorSeedOutcome.NotConfigured;

        if (provided < 3
            || !Username.IsValid(settings.Username)
            || !Email.IsValid(settings.Email)
            || !IsAcceptablePassword(settings.Password))
        {
            logger.LogError(
                "The Coordinator seed is not valid and was skipped: it needs Username, an institutional (.edu.pe) " +
                "Email and a Password of at least {MinLength} characters.", MinPasswordLength);
            return CoordinatorSeedOutcome.Invalid;
        }

        var username = new Username(settings.Username!);
        var email = new Email(settings.Email!);

        if (await userRepository.ExistsByUsernameAsync(username, cancellationToken)
            || await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            logger.LogInformation("An account for the Coordinator seed {Username} already exists; it was left " +
                                  "untouched.", username.Value);
            return CoordinatorSeedOutcome.AlreadyExists;
        }

        var coordinator = new User(username, email, passwordHasher.HashPassword(settings.Password!),
            UserRole.Coordinator);
        await userRepository.AddAsync(coordinator, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        logger.LogInformation("The Coordinator account {Username} was created.", username.Value);
        return CoordinatorSeedOutcome.Created;
    }

    private static bool IsAcceptablePassword(string? password)
    {
        return password is not null
               && password.Length >= MinPasswordLength
               && Encoding.UTF8.GetByteCount(password) <= MaxPasswordBytes;
    }
}