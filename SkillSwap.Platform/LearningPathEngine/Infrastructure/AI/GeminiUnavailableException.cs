namespace SkillSwap.Platform.LearningPathEngine.Infrastructure.AI;

/// <summary>
///     A Gemini model could not serve the request right now: it is overloaded, rate limited, failing or
///     no longer available. Another attempt or another model may work. Permanent errors (invalid key,
///     bad request...) are plain <see cref="InvalidOperationException" />s instead.
/// </summary>
/// <param name="message">What the provider answered</param>
/// <param name="isRetryable">
///     Whether trying the same model again can help. A retired model (404) is unavailable but not retryable.
/// </param>
public sealed class GeminiUnavailableException(string message, bool isRetryable = true)
    : InvalidOperationException(message)
{
    public bool IsRetryable { get; } = isRetryable;
}