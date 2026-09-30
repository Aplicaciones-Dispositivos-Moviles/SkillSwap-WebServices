using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Domain.Model.Commands;
using SkillSwap.Platform.Shared.Application.Model;

namespace SkillSwap.Platform.Iam.Application.CommandServices;

/// <summary>
///     User command service interface
/// </summary>
public interface IUserCommandService
{
    /// <summary>
    ///     Handle sign up command
    /// </summary>
    /// <returns>The <see cref="Result{T}" /> wrapping the created user</returns>
    Task<Result<User>> Handle(SignUpCommand command, CancellationToken cancellationToken);

    /// <summary>
    ///     Handle sign in command
    /// </summary>
    /// <returns>The <see cref="Result{T}" /> wrapping the authenticated user and its JWT</returns>
    Task<Result<(User User, string Token)>> Handle(SignInCommand command, CancellationToken cancellationToken);

    /// <summary>
    ///     Handle update user bio command
    /// </summary>
    /// <returns>The <see cref="Result{T}" /> wrapping the updated user</returns>
    Task<Result<User>> Handle(UpdateUserBioCommand command, CancellationToken cancellationToken);
}