using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SkillSwap.Platform.Iam.Application.CommandServices;
using SkillSwap.Platform.Iam.Application.Internal.OutboundServices;
using SkillSwap.Platform.Iam.Domain.Model;
using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Domain.Model.Commands;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Domain.Repositories;
using SkillSwap.Platform.Iam.Domain.Services;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Domain.Repositories;
using SkillSwap.Platform.Shared.Resources.Errors;
using SkillSwap.Platform.Iam.Domain.Model.Events;
using SkillSwap.Platform.Shared.Domain.Events;

namespace SkillSwap.Platform.Iam.Application.Internal.CommandServices;

/// <summary>
///     User command service
/// </summary>
/// <param name="userRepository">User repository</param>
/// <param name="passwordHasher">Password hasher domain service</param>
/// <param name="emailDomainValidator">Institutional email domain validator</param>
/// <param name="tokenGenerator">Token generator</param>
/// <param name="unitOfWork">Unit of work</param>
/// <param name="eventPublisher">Publishes domain events once the change is saved</param>
/// <param name="localizer">String localizer for error messages</param>
public class UserCommandService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailDomainValidator emailDomainValidator,
    ITokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork,
    IDomainEventPublisher eventPublisher,
    IStringLocalizer<ErrorMessage> localizer)
    : IUserCommandService
{
    private const int MinPasswordLength = 8;
    private const int MaxPasswordBytes = 72; // BCrypt ignores anything beyond 72 bytes

    /// <inheritdoc />
    public async Task<Result<User>> Handle(SignUpCommand command, CancellationToken cancellationToken)
    {
        if (!Username.IsValid(command.Username))
            return Failure<User>(IamError.InvalidUsername);

        if (!emailDomainValidator.IsInstitutionalDomain(command.Email))
            return Failure<User>(IamError.InvalidInstitutionalEmail);

        if (!IsAcceptablePassword(command.Password))
            return Failure<User>(IamError.WeakPassword);

        var username = new Username(command.Username);
        var email = new Email(command.Email);

        if (await userRepository.ExistsByUsernameAsync(username, cancellationToken))
            return Failure<User>(IamError.UsernameAlreadyTaken);

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
            return Failure<User>(IamError.EmailAlreadyTaken);

        var user = new User(username, email, passwordHasher.HashPassword(command.Password), command.Role);
        await userRepository.AddAsync(user, cancellationToken);

        var error = await TrySaveAsync(cancellationToken);
        if (error is not null) return Failure<User>(error.Value);

        // Other bounded contexts (the wallet, later the free plan) react to the new account.
        await eventPublisher.PublishAsync(new UserRegistered(user.Id, user.Role), cancellationToken);
        return Result<User>.Success(user);
    }

    /// <inheritdoc />
    public async Task<Result<(User User, string Token)>> Handle(SignInCommand command,
        CancellationToken cancellationToken)
    {
        // Same error for unknown user and wrong password, so the API does not reveal which usernames exist.
        if (!Username.IsValid(command.Username))
            return Failure<(User User, string Token)>(IamError.InvalidCredentials);

        var user = await userRepository.FindByUsernameAsync(new Username(command.Username), cancellationToken);
        if (user is null || !passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
            return Failure<(User User, string Token)>(IamError.InvalidCredentials);

        var token = tokenGenerator.GenerateToken(user);
        return Result<(User User, string Token)>.Success((user, token));
    }

    /// <inheritdoc />
    public async Task<Result<User>> Handle(UpdateUserBioCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Failure<User>(IamError.UserNotFound);

        if (user.Id != command.ActorUserId)
            return Failure<User>(IamError.NotProfileOwner);

        if (command.Bio.Trim().Length > User.MaxBioLength)
            return Failure<User>(IamError.BioTooLong);

        user.UpdateBio(command.Bio);
        userRepository.Update(user);

        var error = await TrySaveAsync(cancellationToken);
        return error is null ? Result<User>.Success(user) : Failure<User>(error.Value);
    }

    private static bool IsAcceptablePassword(string? password)
    {
        return password is not null
               && password.Length >= MinPasswordLength
               && Encoding.UTF8.GetByteCount(password) <= MaxPasswordBytes;
    }

    private Result<T> Failure<T>(IamError error)
    {
        return Result<T>.Failure(error, localizer[error.ToString()]);
    }

    private async Task<IamError?> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.CompleteAsync(cancellationToken);
            return null;
        }
        catch (OperationCanceledException)
        {
            return IamError.OperationCancelled;
        }
        catch (DbUpdateException)
        {
            return IamError.DatabaseError;
        }
        catch (Exception)
        {
            return IamError.InternalServerError;
        }
    }
}