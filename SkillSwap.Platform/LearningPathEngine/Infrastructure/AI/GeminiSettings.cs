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
    ///     Optional reasoning effort ("low", ...). When empty, the parameter is not sent and the model
    ///     decides. Gemini 3 models spend many tokens "thinking" by default.
    /// </summary>
    public string? ThinkingLevel { get; set; }

    public int TimeoutSeconds { get; set; } = 60;

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
}