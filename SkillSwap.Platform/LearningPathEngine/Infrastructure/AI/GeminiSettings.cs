namespace SkillSwap.Platform.LearningPathEngine.Infrastructure.AI;

/// <summary>
///     Gemini settings, bound from the "Gemini" configuration section. The API key never goes in the
///     repository: use appsettings.Development.json locally (it is git-ignored) and environment variables
///     in production (Gemini__ApiKey, Gemini__Model, Gemini__FallbackModels__0, Gemini__FallbackModels__1...).
/// </summary>
public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-3.5-flash";

    /// <summary>
    ///     Models tried, in order, when the main one cannot serve the request (overloaded, rate limited,
    ///     not answering in time or retired). Empty means no fallback.
    /// </summary>
    public List<string> FallbackModels { get; set; } = [];

    /// <summary>
    ///     Optional reasoning effort ("low", ...). When empty, the parameter is not sent. A model that
    ///     rejects it is retried without it.
    /// </summary>
    public string? ThinkingLevel { get; set; }

    /// <summary>
    ///     Maximum time to wait for each attempt.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    ///     Maximum time for the whole operation, however many retries and models are tried.
    /// </summary>
    public int TotalTimeoutSeconds { get; set; } = 60;

    /// <summary>
    ///     Retries per model after a transient failure (429, 500, 502, 503, 504).
    /// </summary>
    public int MaxRetries { get; set; } = 1;

    /// <summary>
    ///     Wait before the first retry; it doubles on each following one.
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
}