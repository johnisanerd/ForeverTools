# ForeverTools.OpenAI

A clean, dependency-injection-ready .NET wrapper for the [OpenAI API](https://platform.openai.com) — chat completions, embeddings, and image generation in one simple client.

## Features

- **Chat Completions** — simple one-shot, multi-turn, and streaming
- **Embeddings** — single and batch embedding generation for RAG and semantic search
- **Image Generation** — DALL-E 3 and DALL-E 2, multiple sizes
- **Dependency Injection** — built-in `IServiceCollection` extensions
- **Multi-targeting** — .NET 8.0, .NET 6.0, .NET Standard 2.0
- **Full async/await** with cancellation token support

## Installation

```bash
dotnet add package ForeverTools.OpenAI
```

## Quick Start

Get your API key at [platform.openai.com/api-keys](https://platform.openai.com/api-keys).

```csharp
using ForeverTools.OpenAI;

var client = new OpenAiClient("your-api-key");

// Chat completion
var reply = await client.ChatAsync("What is the capital of France?");
Console.WriteLine(reply); // "The capital of France is Paris."

// Streaming
await foreach (var chunk in client.StreamChatAsync("Tell me a joke"))
    Console.Write(chunk);

// Embeddings (for RAG / semantic search)
var vector = await client.EmbedAsync("Hello world");
Console.WriteLine($"Dimension: {vector.Length}"); // 1536

// Image generation
var url = await client.GenerateImageAsync("A sunset over mountains");
Console.WriteLine(url);
```

## Dependency Injection

```csharp
// Program.cs
builder.Services.AddForeverToolsOpenAI("your-api-key");

// Or from configuration
builder.Services.AddForeverToolsOpenAI(builder.Configuration);
```

```json
// appsettings.json
{
  "OpenAI": {
    "ApiKey": "your-api-key",
    "DefaultChatModel": "gpt-4o-mini",
    "DefaultImageModel": "dall-e-3",
    "DefaultEmbeddingModel": "text-embedding-3-small"
  }
}
```

Then inject `OpenAiClient` directly:

```csharp
public class MyService(OpenAiClient ai)
{
    public Task<string> SummarizeAsync(string text) =>
        ai.ChatAsync($"Summarise this in one sentence: {text}");
}
```

## Available Models

```csharp
// Chat
OpenAiModels.Chat.Gpt4o          // gpt-4o
OpenAiModels.Chat.Gpt4oMini      // gpt-4o-mini (default)
OpenAiModels.Chat.Gpt4Turbo      // gpt-4-turbo
OpenAiModels.Chat.O3Mini         // o3-mini

// Images
OpenAiModels.Image.DallE3        // dall-e-3 (default)
OpenAiModels.Image.DallE2        // dall-e-2

// Embeddings
OpenAiModels.Embedding.TextEmbedding3Small  // default
OpenAiModels.Embedding.TextEmbedding3Large
```

## Multi-turn Conversations

```csharp
using OpenAI.Chat;

var messages = new List<ChatMessage>
{
    new SystemChatMessage("You are a helpful assistant."),
    new UserChatMessage("What is 2 + 2?"),
    new AssistantChatMessage("4"),
    new UserChatMessage("And multiply by 3?"),
};

var reply = await client.ChatAsync(messages);
Console.WriteLine(reply); // "12"
```

## Advanced: Direct Client Access

For features not covered by this wrapper, access the underlying `OpenAI.OpenAIClient`:

```csharp
var underlying = client.UnderlyingClient;
var audioClient = underlying.GetAudioClient("whisper-1");
```

---

## Other ForeverTools Packages

| Package | Description |
|---------|-------------|
| [ForeverTools.AIML](https://www.nuget.org/packages/ForeverTools.AIML) | 400+ models via AI/ML API (GPT-4, Claude, Llama, Gemini) |
| [ForeverTools.ImageGen](https://www.nuget.org/packages/ForeverTools.ImageGen) | AI image generation (DALL-E, Flux, SD) |
| [ForeverTools.Translate](https://www.nuget.org/packages/ForeverTools.Translate) | AI translation, 100+ languages |
| [ForeverTools.OCR](https://www.nuget.org/packages/ForeverTools.OCR) | AI-powered OCR via GPT-4 Vision |

---

## Need Utility APIs?

[**kiprio.com**](https://kiprio.com) offers 30+ utility APIs with a free tier:
email validation, DNS lookup, WHOIS, SSL checks, IP geolocation, OCR, QR codes,
screenshots, sentiment analysis, translation, grammar checking, and more.

```bash
pip install kiprio   # Python SDK
dotnet add package ForeverTools.OpenAI   # .NET SDK (this package)
```

---

## License

MIT — see [LICENSE](https://github.com/ForeverTools/ForeverTools/blob/main/LICENSE).
