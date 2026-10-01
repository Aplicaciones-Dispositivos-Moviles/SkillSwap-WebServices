namespace SkillSwap.Platform.LearningPathEngine.Infrastructure.AI;

/// <summary>
///     Gemini settings, bound from the "Gemini" configuration section. The API key never goes in the
///     repository: use appsettings.Development.json locally (it is git-ignored) and the environment
///     variables Gemini__ApiKey and Gemini__Model in production.
/// </summary>
public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-3.5-flash";

    /// <summary>
    ///     Optional second model, tried when the main one stays unavailable (overloaded, rate limited or
    ///     failing) after all its retries. When empty, there is no fallback.
    /// </summary>
    public string? FallbackModel { get; set; }

    /// <summary>
    ///     Optional reasoning effort ("low", ...). When empty, the parameter is not sent and the model
    ///     decides. Gemini 3 models spend many tokens "thinking" by default.
    /// </summary>
    public string? ThinkingLevel { get; set; }

    /// <summary>
    ///     Maximum time to wait for each attempt.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    ///     Retries per model after a transient failure (429, 500, 502, 503, 504).
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    ///     Wait before the first retry; it doubles on each following one.
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
}