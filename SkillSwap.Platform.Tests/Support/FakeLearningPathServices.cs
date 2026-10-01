using SkillSwap.Platform.CredentialVerification.Application.ACL;
using SkillSwap.Platform.LearningPathEngine.Application.Internal.OutboundServices;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Entities;
using SkillSwap.Platform.LearningPathEngine.Domain.Services;

namespace SkillSwap.Platform.Tests.Support;

/// <summary>
///     Matches a text to skills when it contains a phrase (case-insensitive).
/// </summary>
public class FakeSkillTaxonomyMatcher(params (string Phrase, string Tag)[] rules) : ISkillTaxonomyMatcher
{
    public static FakeSkillTaxonomyMatcher Sample()
    {
        return new FakeSkillTaxonomyMatcher(
            ("jwt", "authentication-jwt"),
            ("rest", "rest-api-design"),
            ("http", "http-basics"),
            ("sql", "sql-fundamentals"));
    }

    public IReadOnlyList<string> Match(string text)
    {
        return rules.Where(rule => text.Contains(rule.Phrase, StringComparison.OrdinalIgnoreCase))
            .Select(rule => rule.Tag).Distinct().ToList();
    }
}

public class FakeCredentialContextFacade : ICredentialContextFacade
{
    public List<CertificateSummary> Certificates { get; } = [];

    public Exception? ExceptionToThrow { get; set; }

    public Task<IReadOnlyList<CertificateSummary>> GetEvidenceCertificatesAsync(int ownerId,
        CancellationToken cancellationToken)
    {
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult<IReadOnlyList<CertificateSummary>>(Certificates.OrderBy(c => c.Id).ToList());
    }
}

public class FakeQuestionGenerationService : IQuestionGenerationService
{
    private int _calls;

    public List<string> Requests { get; } = [];

    /// <summary>
    ///     When set, <see cref="GenerateQuestionsAsync" /> throws it, simulating a provider failure.
    /// </summary>
    public Exception? ExceptionToThrow { get; set; }

    public int QuestionCount { get; set; } = AssessmentBlueprint.QuestionCount;

    public Task<IReadOnlyList<Question>> GenerateQuestionsAsync(string skillTag, CancellationToken cancellationToken)
    {
        if (ExceptionToThrow is not null) throw ExceptionToThrow;

        Requests.Add(skillTag);
        var offset = _calls++ * 10;
        IReadOnlyList<Question> questions = Enumerable.Range(1, QuestionCount)
            .Select(i => LearningPathTestData.Question(offset + i)).ToList();
        return Task.FromResult(questions);
    }
    public void Reset()
    {
        ExceptionToThrow = null;
        QuestionCount = AssessmentBlueprint.QuestionCount;
        Requests.Clear();
        _calls = 0;
    }
}