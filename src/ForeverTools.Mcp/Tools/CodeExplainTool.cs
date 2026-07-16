using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace ForeverTools.Mcp.Tools;

[McpServerToolType]
public sealed class CodeExplainTool(IHttpClientFactory httpFactory)
{
    private const string ApiBase = "https://api.aimlapi.com/v1";

    [McpServerTool, Description(
        "Explain, review, or analyse code in plain English. Supports all major programming languages. " +
        "Requires AIMLAPI_KEY environment variable (get one free at aimlapi.com/?via=forevertools).")]
    public async Task<string> ExplainCode(
        [Description("The code snippet to explain or analyse")] string code,
        [Description("Mode: 'explain' (plain English explanation), 'review' (code review with issues), 'complexity' (time/space complexity), 'refactor' (suggest improvements), 'document' (generate docstring/comments)")] string mode = "explain",
        [Description("Programming language (optional, auto-detected if omitted)")] string? language = null,
        [Description("Detail level: 'beginner' (step by step), 'intermediate' (standard), 'expert' (deep technical)")] string level = "intermediate")
    {
        var apiKey = Environment.GetEnvironmentVariable("AIMLAPI_KEY")
            ?? throw new InvalidOperationException("AIMLAPI_KEY environment variable not set. Get a free key at https://aimlapi.com/?via=forevertools");

        if (code.Length > 8_000)
            code = code[..8_000];

        var langHint = language != null ? $" The language is {language}." : "";

        var instruction = mode.ToLower() switch
        {
            "review"     => $"Review this code for bugs, security issues, performance problems, and style issues.{langHint} Provide specific, actionable feedback.",
            "complexity" => $"Analyse the time and space complexity of this code.{langHint} Explain Big-O notation for key operations. Identify any bottlenecks.",
            "refactor"   => $"Suggest concrete refactoring improvements for this code.{langHint} Show improved snippets where helpful. Focus on readability, maintainability, and performance.",
            "document"   => $"Generate clear docstrings/comments for this code.{langHint} Follow idiomatic documentation style for the language.",
            _            => $"Explain what this code does in plain English.{langHint} Tailor the explanation for a {level}-level developer."
        };

        var systemPrompt = level switch
        {
            "beginner" => "You are a patient programming teacher. Explain concepts step-by-step, avoid jargon, and use simple analogies.",
            "expert"   => "You are a senior software engineer. Be technically precise, reference design patterns and performance implications.",
            _          => "You are a helpful code reviewer. Be clear and practical."
        };

        var payload = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"{instruction}\n\n```\n{code}\n```" }
            },
            max_tokens = 1200
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
