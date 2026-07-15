using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace ForeverTools.Mcp.Tools;

[McpServerToolType]
public sealed class JsonTool
{
    [McpServerTool, Description(
        "Format, validate, and transform JSON. Supports pretty-printing (2-space indent), " +
        "minifying, and sorting keys alphabetically. Returns an error message if the input is not valid JSON. " +
        "No API key required — pure .NET processing.")]
    public string FormatJson(
        [Description("The JSON string to format")] string json,
        [Description("Output mode: 'pretty' (indented, default), 'minify' (compact), 'sort' (pretty + keys sorted alphabetically)")] string mode = "pretty")
    {
        if (string.IsNullOrWhiteSpace(json))
            return string.Empty;

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        }
        catch (JsonException ex)
        {
            return $"Invalid JSON: {ex.Message}";
        }

        using (doc)
        {
            return mode.ToLowerInvariant() switch
            {
                "minify" => JsonSerializer.Serialize(doc.RootElement),
                "sort"   => SerializeSorted(doc.RootElement),
                _        => JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true }),
            };
        }
    }

    private static string SerializeSorted(JsonElement element)
    {
        var sorted = ToSortedObject(element);
        return JsonSerializer.Serialize(sorted, new JsonSerializerOptions { WriteIndented = true });
    }

    private static object? ToSortedObject(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject()
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(p => p.Name, p => ToSortedObject(p.Value)),
        JsonValueKind.Array  => element.EnumerateArray().Select(ToSortedObject).ToArray(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var l) ? (object?)l : element.GetDouble(),
        JsonValueKind.True   => true,
        JsonValueKind.False  => false,
        _                    => null,
    };
}
