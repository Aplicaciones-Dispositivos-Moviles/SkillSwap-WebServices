using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SkillSwap.Platform.Iam.Application.Internal.OutboundServices;
using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;

namespace SkillSwap.Platform.Iam.Infrastructure.Tokens.Jwt.Services;

/// <summary>
///     Generates and validates JWTs signed with a shared secret. The token encodes the user id and role.
/// </summary>
public class JwtTokenGenerator : ITokenGenerator
{
    private const int MinSecretLength = 32;

    private readonly TokenSettings _settings;
    private readonly SymmetricSecurityKey _key;

    public JwtTokenGenerator(IOptions<TokenSettings> tokenSettings)
    {
        _settings = tokenSettings.Value;
        if (string.IsNullOrWhiteSpace(_settings.Secret) || _settings.Secret.Length < MinSecretLength)
            throw new InvalidOperationException(
                $"TokenSettings:Secret must be configured with at least {MinSecretLength} characters.");
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
    }

    /// <inheritdoc />
    public string GenerateToken(User user)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Sid, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username.Value),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            ]),
            Expires = DateTime.UtcNow.AddDays(_settings.ExpirationDays),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    /// <inheritdoc />
    public async Task<int?> ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        try
        {
            var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            });

            if (!result.IsValid) return null;

            var sid = result.ClaimsIdentity.FindFirst(ClaimTypes.Sid)?.Value;
            return int.TryParse(sid, out var userId) ? userId : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}