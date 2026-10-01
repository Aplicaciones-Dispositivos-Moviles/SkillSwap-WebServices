namespace SkillSwap.Platform.LearningPathEngine.Infrastructure.AI;

/// <summary>
///     The AI provider could not serve the request right now (overloaded, rate limited or failing), so
///     trying again later may work. Permanent errors (invalid key, unknown model...) are plain
///     <see cref="InvalidOperationException" />s instead.
/// </summary>
public sealed class GeminiUnavailableException(string message) : InvalidOperationException(message);