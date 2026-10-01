using System.Text.Json;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.CredentialVerification.Integration;

public class OpenApiDocumentTests
{
    [Fact]
    public async Task IssueDate_IsDocumentedAsADateString()
    {
        var json = await TestApi.CreateClient().GetStringAsync("/swagger/v1/swagger.json");
        using var document = JsonDocument.Parse(json);

        var schemas = FindProperties(document.RootElement, "issueDate").ToList();

        Assert.NotEmpty(schemas);
        Assert.All(schemas, schema =>
        {
            Assert.Equal("string", schema.GetProperty("type").GetString());
            Assert.Equal("date", schema.GetProperty("format").GetString());
        });
    }

    private static IEnumerable<JsonElement> FindProperties(JsonElement element, string name)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Object &&
                        string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                        yield return property.Value;

                    foreach (var nested in FindProperties(property.Value, name)) yield return nested;
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                foreach (var nested in FindProperties(item, name))
                    yield return nested;
                break;
        }
    }
}