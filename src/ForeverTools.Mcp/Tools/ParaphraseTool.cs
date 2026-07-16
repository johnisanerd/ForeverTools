using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace ForeverTools.Mcp.Tools;

[McpServerToolType]
public sealed class ParaphraseTool(IHttpClientFactory httpFactory)
{
    private const string ApiBase = "https://api.aimlapi.com/v1";

    [McpServerTool, Description(
        "Paraphrase or rewrite text in a different style or tone. Preserves meaning while changing wording. " +
        "Requires AIMLAPI_KEY environment variable (get one free at aimlapi.com/?via=forevertools).")]
    public async Task<string> Paraphrase(
        [Description("The text to paraphrase")] string text,
        [Description("Target tone/style: 'formal' (professional), 'casual' (friendly), 'concise' (shorter), 'expand' (more detailed), 'simple' (plain English), 'academic' (scholarly)")] string style = "formal",
        [Description("Number of alternative versions to return (1-3, default 1)")] int variants = 1)
    {
        var apiKey = Environment.GetEnvironmentVariable("AIMLAPI_KEY")
            ?? throw new InvalidOperationException("AIMLAPI_KEY environment variable not set. Get a free key at https://aimlapi.com/?via=forevertools");

        if (text.Length > 5_000)
            text = text[..5_000];

        var styleInstruction = style.ToLower() switch
        {
            "casual"   => "Rewrite in a friendly, conversational tone suitable for social media or informal communication.",
            "concise"  => "Rewrite more concisely — reduce length by at least 30% while preserving all key meaning.",
            "expand"   => "Expand the text with more detail, context, and explanation while staying on topic.",
            "simple"   => "Rewrite in plain English, avoiding jargon. Target a reading age of 12-14 years.",
            "academic" => "Rewrite in a formal academic style suitable for research papers or scholarly articles.",
            _          => "Rewrite in a professional, formal tone suitable for business communication."
        };

        var variantText = variants > 1 ? $" Provide {variants} distinct alternatives, numbered." : "";

        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = $"You are an expert editor and writer. {styleInstruction}{variantText} Return ONLY the rewritten text, no explanations or preamble." },
                new { role = "user", content = text }
            },
            max_tokens = Math.Min(text.Length * 4, 2000)
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
