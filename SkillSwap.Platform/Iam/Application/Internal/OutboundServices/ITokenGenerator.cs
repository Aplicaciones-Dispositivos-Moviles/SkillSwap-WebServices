using SkillSwap.Platform.Iam.Domain.Model.Aggregates;

namespace SkillSwap.Platform.Iam.Application.Internal.OutboundServices;

/// <summary>
///     Contract for generating and validating the access tokens of authenticated sessions.
/// </summary>
public interface ITokenGenerator
{
    /// <summary>
    ///     Generate a token encoding the user id and role.
    /// </summary>
    string GenerateToken(User user);

    /// <summary>
    ///     Validate a token.
    /// </summary>
    /// <returns>The user id encoded in the token if it is valid; otherwise null.</returns>
    Task<int?> ValidateToken(string token);
}