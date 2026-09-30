using Microsoft.Extensions.Localization;
using SkillSwap.Platform.Iam.Application.Internal.OutboundServices;
using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Domain.Services;

namespace SkillSwap.Platform.Tests.Support;

/// <summary>
///     Returns the resource key as the message, so tests don't depend on the translations.
/// </summary>
public class FakeLocalizer<T> : IStringLocalizer<T>
{
    public LocalizedString this[string name] => new(name, name);

    public LocalizedString this[string name, params object[] arguments] => new(name, name);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return [];
    }
}

public class FakePasswordHasher : IPasswordHasher
{
    public PasswordHash HashPassword(string plainPassword)
    {
        return new PasswordHash($"hashed:{plainPassword}");
    }

    public bool VerifyPassword(string plainPassword, PasswordHash hash)
    {
        return hash.Value == $"hashed:{plainPassword}";
    }
}

public class FakeTokenGenerator : ITokenGenerator
{
    public string GenerateToken(User user)
    {
        return $"token-for-{user.Id}";
    }

    public Task<int?> ValidateToken(string token)
    {
        return Task.FromResult<int?>(null);
    }
}