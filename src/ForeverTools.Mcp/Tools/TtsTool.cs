using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace ForeverTools.Mcp.Tools;

[McpServerToolType]
public sealed class TtsTool(IHttpClientFactory httpFactory)
{
    private const string ApiBase = "https://api.aimlapi.com/v1";

    private static readonly Dictionary<string, string> Voices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["alloy"]   = "Neutral, balanced (default)",
        ["echo"]    = "Male, clear",
        ["fable"]   = "British, expressive",
        ["onyx"]    = "Male, deep and authoritative",
        ["nova"]    = "Female, warm and friendly",
        ["shimmer"] = "Female, soft and calm",
    };

    [McpServerTool, Description(
        "Convert text to speech and save as an MP3 file. Returns the path to the saved audio file. " +
        "Requires AIMLAPI_KEY environment variable (get one free at aimlapi.com/?via=forevertools).")]
    public async Task<string> TextToSpeech(
        [Description("The text to convert to speech (up to 4,000 characters)")] string text,
        [Description("Voice: 'alloy' (neutral), 'echo' (male), 'fable' (British), 'onyx' (deep male), 'nova' (friendly female), 'shimmer' (calm female)")] string voice = "alloy",
        [Description("Output file path (optional). Defaults to a temp file. Use .mp3 extension.")] string? outputPath = null,
        [Description("Speaking speed (0.25 to 4.0, default 1.0)")] double speed = 1.0)
    {
        var apiKey = Environment.GetEnvironmentVariable("AIMLAPI_KEY")
            ?? throw new InvalidOperationException("AIMLAPI_KEY environment variable not set. Get a free key at https://aimlapi.com/?via=forevertools");

        if (text.Length > 4_000)
            text = text[..4_000];

        if (!Voices.ContainsKey(voice))
            voice = "alloy";

        speed = Math.Clamp(speed, 0.25, 4.0);

        var payload = new
        {
            model = "tts-1",
            input = text,
            voice,
            speed,
            response_format = "mp3"
        };

        using var client = httpFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var response = await client.PostAsJsonAsync($"{ApiBase}/audio/speech", payload);
        response.EnsureSuccessStatusCode();

        var audioBytes = await response.Content.ReadAsByteArrayAsync();

        var path = outputPath ?? Path.Combine(Path.GetTempPath(), $"tts_{DateTime.UtcNow:yyyyMMdd_HHmmss}.mp3");
        path = Path.GetFullPath(path);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, audioBytes);

        return $"Audio saved to: {path} ({audioBytes.Length / 1024} KB, voice: {voice}, speed: {speed}x)";
    }

    [McpServerTool, Description("List available text-to-speech voices.")]
    public Task<string> ListVoices() => Task.FromResult(string.Join("\n",
        Voices.Select(kv => $"• {kv.Key,-10} — {kv.Value}")));
}
