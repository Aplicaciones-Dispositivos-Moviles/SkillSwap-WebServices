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

        var schemas = FindSchemaProperties(document.RootElement, "issueDate").ToList();

        Assert.NotEmpty(schemas);
        Assert.All(schemas, schema =>
        {
            Assert.Equal("string", schema.GetProperty("type").GetString());
            Assert.Equal("date", schema.GetProperty("format").GetString());
        });
    }

    /// <summary>
    ///     Finds the schema of a property only where schemas define their properties. Other places of
    ///     the document can reuse the same name (e.g. the multipart "encoding" section) without being schemas.
    /// </summary>
    private static IEnumerable<JsonElement> FindSchemaProperties(JsonElement element, string name)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals("properties") && property.Value.ValueKind == JsonValueKind.Object)
                        foreach (var child in property.Value.EnumerateObject())
                            if (string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
                                yield return child.Value;

                    foreach (var nested in FindSchemaProperties(property.Value, name)) yield return nested;
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                foreach (var nested in FindSchemaProperties(item, name))
                    yield return nested;
                break;
        }
    }
}