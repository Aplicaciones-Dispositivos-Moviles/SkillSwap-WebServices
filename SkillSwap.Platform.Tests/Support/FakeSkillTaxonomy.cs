using SkillSwap.Platform.LearningPathEngine.Domain.Services;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.Support;

public class FakeSkillTaxonomy(IReadOnlyDictionary<string, string[]> prerequisites) : ISkillTaxonomy
{
    /// <summary>
    ///     programming-fundamentals, networking-basics -> http-basics -> rest-api-design -> authentication-jwt
    ///     (rest-api-design also requires programming-fundamentals), plus the independent sql-fundamentals.
    /// </summary>
    public static FakeSkillTaxonomy Sample()
    {
        return new FakeSkillTaxonomy(new Dictionary<string, string[]>
        {
            ["programming-fundamentals"] = [],
            ["networking-basics"] = [],
            ["http-basics"] = ["networking-basics"],
            ["rest-api-design"] = ["http-basics", "programming-fundamentals"],
            ["authentication-jwt"] = ["rest-api-design"],
            ["sql-fundamentals"] = []
        });
    }

    public bool Contains(string skillTag)
    {
        return prerequisites.ContainsKey(skillTag);
    }

    public IReadOnlyList<string> PrerequisitesOf(string skillTag)
    {
        return prerequisites.TryGetValue(skillTag, out var result)
            ? result
            : throw new DomainException($"The skill '{skillTag}' is not in the taxonomy.");
    }
}