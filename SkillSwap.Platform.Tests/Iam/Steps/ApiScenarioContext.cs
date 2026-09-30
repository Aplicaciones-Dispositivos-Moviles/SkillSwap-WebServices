using System.Text.Json;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Iam.Steps;

/// <summary>
///     State shared by the step classes of one scenario (injected by Reqnroll).
/// </summary>
public class ApiScenarioContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public HttpResponseMessage? Response { get; set; }
    public string? AcceptLanguage { get; set; }
    public Dictionary<string, SignedInUser> Users { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Deserializes the last response body. Safe to call several times in the same scenario,
    ///     unlike ReadFromJsonAsync, which closes the content stream after the first read.
    /// </summary>
    public async Task<T> ReadBodyAsync<T>()
    {
        var body = await Response!.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(body, JsonOptions)!;
    }
}