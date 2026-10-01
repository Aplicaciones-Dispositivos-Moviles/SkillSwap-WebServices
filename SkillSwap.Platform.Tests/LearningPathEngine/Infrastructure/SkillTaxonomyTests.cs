using SkillSwap.Platform.LearningPathEngine.Infrastructure.Taxonomy;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Infrastructure;

public class SkillTaxonomyTests
{
    private static readonly SkillCatalog Catalog = SkillCatalog.LoadEmbedded();
    private static readonly SkillTaxonomyMatcher Matcher = new(Catalog);
    private static readonly JsonSkillTaxonomy Taxonomy = new(Catalog);

    private static string[] Sorted(IEnumerable<string> tags)
    {
        return tags.OrderBy(tag => tag, StringComparer.Ordinal).ToArray();
    }

    // ---------- Loading the embedded catalog ----------

    [Fact]
    public void EmbeddedCatalog_LoadsAndIsNotEmpty()
    {
        Assert.True(Catalog.Skills.Count >= 40);
        Assert.True(Catalog.TryGet("rest-api-design", out var skill));
        Assert.Equal("REST API design", skill.Name);
    }

    // ---------- Parsing and validation ----------

    private static string Json(string skills)
    {
        return $$"""{"version": 1, "skills": [{{skills}}]}""";
    }

    private static string Skill(string tag, string prerequisites = "", string keywords = "\"kw\"", string name = "Name")
    {
        return $$"""{"tag": "{{tag}}", "name": "{{name}}", "category": "web", "keywords": [{{keywords}}], "prerequisites": [{{prerequisites}}]}""";
    }

    [Fact]
    public void Parse_WithAValidCatalog_Succeeds()
    {
        var catalog = SkillCatalog.Parse(Json($"{Skill("a")},{Skill("b", "\"a\"", "\"other\"")}"));

        Assert.Equal(2, catalog.Skills.Count);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"version\": 1, \"skills\": []}")]
    [InlineData("{\"version\": 1}")]
    public void Parse_WithAnUnusableDocument_Throws(string json)
    {
        Assert.Throws<InvalidOperationException>(() => SkillCatalog.Parse(json));
    }

    [Fact]
    public void Parse_WithARepeatedTag_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            SkillCatalog.Parse(Json($"{Skill("a")},{Skill("a", keywords: "\"other\"")}")));

        Assert.Contains("repeated", exception.Message);
    }

    [Fact]
    public void Parse_WithAnUnknownPrerequisite_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            SkillCatalog.Parse(Json(Skill("a", "\"ghost\""))));

        Assert.Contains("ghost", exception.Message);
    }

    [Fact]
    public void Parse_WithASkillThatRequiresItself_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SkillCatalog.Parse(Json(Skill("a", "\"a\""))));
    }

    [Fact]
    public void Parse_WithAPrerequisiteCycle_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            SkillCatalog.Parse(Json($"{Skill("a", "\"b\"")},{Skill("b", "\"a\"", "\"other\"")}")));

        Assert.Contains("cycle", exception.Message);
    }

    [Fact]
    public void Parse_WithASkillWithoutUsableKeywords_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SkillCatalog.Parse(Json(Skill("a", keywords: "\"!!!\", \" \""))));
    }

    [Fact]
    public void Parse_WithABlankName_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SkillCatalog.Parse(Json(Skill("a", name: " "))));
    }

    // ---------- Taxonomy ----------

    [Fact]
    public void Taxonomy_ExposesTheDirectPrerequisites()
    {
        Assert.True(Taxonomy.Contains("rest-api-design"));
        Assert.Equal(["http-basics", "programming-fundamentals"], Taxonomy.PrerequisitesOf("rest-api-design"));
        Assert.Empty(Taxonomy.PrerequisitesOf("git-version-control"));
    }

    [Fact]
    public void Taxonomy_WithAnUnknownSkill_ThrowsAndDoesNotContainIt()
    {
        Assert.False(Taxonomy.Contains("cooking"));
        Assert.Throws<DomainException>(() => Taxonomy.PrerequisitesOf("cooking"));
    }

    // ---------- Matcher: goals that must be understood ----------

    [Theory]
    [InlineData("quiero aprender a construir APIs REST con autenticación JWT", "authentication-jwt,rest-api-design")]
    [InlineData("quiero ser analista de datos con Power BI", "data-analysis-bi")]
    [InlineData("quiero desarrollar apps móviles con Flutter", "flutter,mobile-app-fundamentals")]
    [InlineData("Quiero ser desarrollador backend en ASP.NET Core y PostgreSQL",
        "aspnet-core,postgresql,rest-api-design")]
    [InlineData("quiero ser DBA y optimizar consultas", "database-administration")]
    [InlineData("quiero aprender Spring Boot con Java", "java-language,spring-boot")]
    [InlineData("I want to learn Docker and CI/CD", "ci-cd-deployment,docker-containers")]
    [InlineData("quiero hacer pruebas unitarias", "software-testing")]
    [InlineData("quiero aprender UX", "ux-ui-design")]
    [InlineData("quiero programar en C#", "csharp-language")]
    [InlineData("quiero hacer un login", "authentication-jwt")]
    public void Match_FindsTheSkillsOfTheGoal(string goal, string expectedTags)
    {
        Assert.Equal(expectedTags.Split(','), Sorted(Matcher.Match(goal)));
    }

    [Fact]
    public void Match_IgnoresCaseAndAccents()
    {
        Assert.Equal(["authentication-jwt"], Matcher.Match("AUTENTICACIÓN"));
        Assert.Equal(["authentication-jwt"], Matcher.Match("autenticacion"));
    }

    [Fact]
    public void Match_OnlyMatchesWholeWords()
    {
        // "java" must not be found inside "javascript", nor "git" inside "digital".
        Assert.Equal(["javascript"], Matcher.Match("quiero aprender JavaScript"));
        Assert.Empty(Matcher.Match("transformación digital"));
    }

    // ---------- Matcher: ambiguous words must not match on their own ----------

    [Theory]
    [InlineData("quiero cocinar pasteles")]
    [InlineData("quiero aprender seguridad")]
    [InlineData("quiero trabajar con la nube")]
    [InlineData("quiero aprender permisos")]
    [InlineData("quiero reservar una room")]
    [InlineData("quiero aprender excel")]
    [InlineData("quiero hacer testing")]
    [InlineData("quiero entrenar para un sprint")]
    [InlineData("")]
    [InlineData("   ")]
    public void Match_DoesNotInterpretAmbiguousOrUnrelatedText(string text)
    {
        Assert.Empty(Matcher.Match(text));
    }

    [Fact]
    public void Match_IsDeterministicAndFollowsTheCatalogOrder()
    {
        var first = Matcher.Match("JWT y REST");
        var second = Matcher.Match("JWT y REST");

        Assert.Equal(first, second);
        Assert.Equal(["rest-api-design", "authentication-jwt"], first);
    }
}