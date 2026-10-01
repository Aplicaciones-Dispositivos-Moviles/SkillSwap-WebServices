using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Entities;
using SkillSwap.Platform.LearningPathEngine.Domain.Services;

namespace SkillSwap.Platform.LearningPathEngine.Infrastructure.AI;

/// <summary>
///     Generates the assessment questions with the Gemini REST API. The model's output is never trusted:
///     every question goes through the <see cref="Question" /> constructor, so anything that breaks the
///     contract (wrong count, repeated answers, missing correct index...) is rejected.
/// </summary>
/// <remarks>
///     Transient provider failures (429 and 5xx) are retried and, when configured, the request moves to a
///     fallback model. Timeouts and permanent errors are not retried.
/// </remarks>
public partial class GeminiQuestionGenerator : IQuestionGenerationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly HashSet<HttpStatusCode> TransientStatuses =
    [
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    ];

    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiQuestionGenerator> _logger;
    private readonly GeminiSettings _settings;

    public GeminiQuestionGenerator(HttpClient httpClient, IOptions<GeminiSettings> options,
        ILogger<GeminiQuestionGenerator> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = options.Value;
        _httpClient.BaseAddress ??= new Uri(_settings.BaseUrl.EndsWith('/') ? _settings.BaseUrl : _settings.BaseUrl + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    private bool HasFallback =>
        !string.IsNullOrWhiteSpace(_settings.FallbackModel)
        && !string.Equals(_settings.FallbackModel, _settings.Model, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Question>> GenerateQuestionsAsync(string skillTag,
        CancellationToken cancellationToken)
    {
        // The tag goes into the prompt, so only the catalog's lowercase kebab-case format is accepted.
        if (!SkillTagPattern().IsMatch(skillTag ?? string.Empty))
            throw new ArgumentException("The skill tag is not valid.", nameof(skillTag));

        var body = BuildRequestBody(skillTag);

        string answerText;
        try
        {
            answerText = await RequestWithRetriesAsync(_settings.Model, body, cancellationToken);
        }
        catch (GeminiUnavailableException exception) when (HasFallback)
        {
            _logger.LogWarning(exception,
                "Gemini model {Model} is unavailable; trying the fallback model {FallbackModel}",
                _settings.Model, _settings.FallbackModel);
            answerText = await RequestWithRetriesAsync(_settings.FallbackModel!, body, cancellationToken);
        }

        return ParseQuestions(answerText);
    }

    private string BuildRequestBody(string skillTag)
    {
        var generationConfig = new Dictionary<string, object> { ["responseMimeType"] = "application/json" };
        if (!string.IsNullOrWhiteSpace(_settings.ThinkingLevel))
            generationConfig["thinkingConfig"] = new Dictionary<string, object>
                { ["thinkingLevel"] = _settings.ThinkingLevel };

        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = BuildPrompt(skillTag) } } } },
            generationConfig
        };
        return JsonSerializer.Serialize(body, JsonOptions);
    }

    private static string BuildPrompt(string skillTag)
    {
        var skill = skillTag.Replace('-', ' ');
        return $$"""
                 You are an assessment writer for a platform that validates software engineering skills.
                 Write exactly {{AssessmentBlueprint.QuestionCount}} multiple-choice questions that test practical understanding of this skill: "{{skill}}".

                 Rules:
                 - Each question has exactly {{Question.AnswerCount}} answer options and exactly one correct option.
                 - The options must be plausible and clearly different from each other.
                 - Do not use "all of the above" or "none of the above".
                 - Vary the position of the correct option across questions.
                 - The questions must be different from each other and cover different sub-topics of the skill.
                 - Write in English. Keep each question under 300 characters and each option under 150 characters.

                 Respond ONLY with a JSON array of exactly {{AssessmentBlueprint.QuestionCount}} objects, each with this shape:
                 {"question": string, "answers": [string, string, string, string], "correctIndex": number from 0 to 3}
                 """;
    }

    /// <summary>
    ///     Sends the request to one model, retrying (with a doubling wait) only while the provider reports
    ///     a transient failure.
    /// </summary>
    private async Task<string> RequestWithRetriesAsync(string model, string body, CancellationToken cancellationToken)
    {
        for (var attempt = 0;; attempt++)
            try
            {
                return await SendAsync(model, body, attempt + 1, cancellationToken);
            }
            catch (GeminiUnavailableException) when (attempt < _settings.MaxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(_settings.RetryDelayMilliseconds * Math.Pow(2, attempt));
                await Task.Delay(delay, cancellationToken);
            }
    }

    private async Task<string> SendAsync(string model, string body, int attempt, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"models/{Uri.EscapeDataString(model)}:generateContent")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-goog-api-key", _settings.ApiKey);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Gemini model {Model}, attempt {Attempt}: {StatusCode} in {ElapsedMs} ms",
                model, attempt, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);

            if (response.IsSuccessStatusCode) return ExtractText(content);

            var message = $"Gemini model '{model}' responded {(int)response.StatusCode} {response.StatusCode}: " +
                          Truncate(content);
            throw TransientStatuses.Contains(response.StatusCode)
                ? new GeminiUnavailableException(message)
                : new InvalidOperationException(message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The HttpClient timeout, not a cancellation requested by the caller.
            _logger.LogWarning("Gemini model {Model}, attempt {Attempt}: no answer after {ElapsedMs} ms",
                model, attempt, stopwatch.ElapsedMilliseconds);
            throw new TimeoutException("Gemini did not answer in time.");
        }
    }

    private static string ExtractText(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);

        if (!document.RootElement.TryGetProperty("candidates", out var candidates)
            || candidates.ValueKind != JsonValueKind.Array
            || candidates.GetArrayLength() == 0)
            throw new InvalidOperationException("Gemini returned no candidates (the request may have been blocked).");

        var candidate = candidates[0];
        if (candidate.TryGetProperty("finishReason", out var finishReason)
            && finishReason.GetString() is { } reason
            && reason != "STOP")
            throw new InvalidOperationException($"Gemini stopped before completing the answer ({reason}).");

        if (!candidate.TryGetProperty("content", out var content)
            || !content.TryGetProperty("parts", out var parts)
            || parts.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Gemini returned an answer without content.");

        var text = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True) continue;
            if (part.TryGetProperty("text", out var partText) && partText.ValueKind == JsonValueKind.String)
                text.Append(partText.GetString());
        }

        if (text.Length == 0) throw new InvalidOperationException("Gemini returned an empty answer.");
        return text.ToString();
    }

    private static IReadOnlyList<Question> ParseQuestions(string answerText)
    {
        var cleaned = CodeFence().Replace(answerText, string.Empty).Trim();

        List<QuestionDto>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<QuestionDto>>(cleaned, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Gemini did not return the questions as a JSON array.", exception);
        }

        if (items is null || items.Count != AssessmentBlueprint.QuestionCount)
            throw new InvalidOperationException(
                $"Gemini returned {items?.Count ?? 0} questions instead of {AssessmentBlueprint.QuestionCount}.");

        return items.Select(item =>
        {
            // Without this check a missing index would silently become 0.
            if (item.CorrectIndex is null)
                throw new InvalidOperationException("A question has no correct answer index.");
            return new Question(item.Question ?? string.Empty, item.Answers ?? [], item.CorrectIndex.Value);
        }).ToList();
    }

    private static string Truncate(string text)
    {
        return text.Length <= 500 ? text : text[..500] + "...";
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SkillTagPattern();

    [GeneratedRegex(@"^\s*```(?:json)?\s*|\s*```\s*$")]
    private static partial Regex CodeFence();

    private sealed class QuestionDto
    {
        public string? Question { get; set; }
        public List<string>? Answers { get; set; }
        public int? CorrectIndex { get; set; }
    }
}