using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Events;

namespace SkillSwap.Platform.Iam.Domain.Model.Events;

/// <summary>
///     A new account was registered.
/// </summary>
/// <param name="UserId">The new user</param>
/// <param name="Role">The role of the account</param>
public sealed record UserRegistered(int UserId, UserRole Role) : IDomainEvent;