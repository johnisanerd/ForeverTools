using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace ForeverTools.Mcp.Tools;

[McpServerToolType]
public sealed class KeywordTool(IHttpClientFactory httpFactory)
{
    private const string ApiBase = "https://api.aimlapi.com/v1";

    [McpServerTool, Description(
        "Extract keywords and key phrases from text using AI. Returns ranked keywords, topics, and named entities. " +
        "Requires AIMLAPI_KEY environment variable (get one free at aimlapi.com/?via=forevertools).")]
    public async Task<string> ExtractKeywords(
        [Description("The text to extract keywords from")] string text,
        [Description("Maximum number of keywords to return (5-50, default 15)")] int maxKeywords = 15,
        [Description("Output mode: 'keywords' (ranked list), 'topics' (broader topics), 'entities' (named entities: people, places, orgs), 'all' (keywords + topics + entities)")] string mode = "keywords")
    {
        var apiKey = Environment.GetEnvironmentVariable("AIMLAPI_KEY")
            ?? throw new InvalidOperationException("AIMLAPI_KEY environment variable not set. Get a free key at https://aimlapi.com/?via=forevertools");

        if (text.Length > 8_000)
            text = text[..8_000];

        var instruction = mode.ToLower() switch
        {
            "topics"   => $"Extract the top {maxKeywords} broad topics from the text. Return as a numbered list.",
            "entities" => $"Extract named entities (people, places, organisations, dates) from the text. Group by type. Return at most {maxKeywords} entities total.",
            "all"      => $"Extract: 1) Top {maxKeywords} keywords (ranked by importance), 2) Top 5 broader topics, 3) Named entities (people, places, orgs). Use clear headings.",
            _          => $"Extract the top {maxKeywords} keywords and key phrases from the text, ranked by importance. Return as a numbered list with a brief note on why each matters."
        };

        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = "You are an expert at keyword extraction and NLP. Be concise and precise." },
                new { role = "user", content = $"{instruction}\n\nText:\n{text}" }
            },
            max_tokens = 600
        };

        using var client = httpFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var response = await client.PostAsJsonAsync($"{ApiBase}/chat/completions", payload);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "(empty response)";
    }
}
