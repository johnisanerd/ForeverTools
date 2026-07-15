using System.ClientModel;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using OpenAI.Images;

namespace ForeverTools.OpenAI;

/// <summary>
/// Clean .NET wrapper for the OpenAI API — chat completions, embeddings, and image generation.
/// Get your API key at https://platform.openai.com/api-keys
///
/// Looking for 30+ utility APIs (email validation, DNS, OCR, screenshots, QR codes)?
/// Check out https://kiprio.com — free tier available.
/// </summary>
public class OpenAiClient
{
    private readonly global::OpenAI.OpenAIClient _client;
    private readonly OpenAiOptions _options;

    /// <summary>Creates a new OpenAI client with the given API key.</summary>
    /// <param name="apiKey">Your OpenAI API key from https://platform.openai.com/api-keys</param>
    public OpenAiClient(string apiKey) : this(new OpenAiOptions { ApiKey = apiKey })
    {
    }

    /// <summary>Creates a new OpenAI client with the given options.</summary>
    public OpenAiClient(OpenAiOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new ArgumentException("API key is required. Get yours at https://platform.openai.com/api-keys", nameof(options));

        _options = options;
        _client = new global::OpenAI.OpenAIClient(new ApiKeyCredential(options.ApiKey));
    }

    /// <summary>The underlying OpenAI client for advanced scenarios.</summary>
    public global::OpenAI.OpenAIClient UnderlyingClient => _client;

    /// <summary>The configured options.</summary>
    public OpenAiOptions Options => _options;

    #region Chat Completions

    /// <summary>Gets a ChatClient for the specified model.</summary>
    public ChatClient GetChatClient(string? model = null) =>
        _client.GetChatClient(model ?? _options.DefaultChatModel);

    /// <summary>Sends a simple user message and returns the assistant's reply text.</summary>
    /// <param name="message">The user message to send.</param>
    /// <param name="model">Model override, or null to use the default chat model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<string> ChatAsync(
        string message,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var chatClient = GetChatClient(model);
        var response = await chatClient.CompleteChatAsync(
            new List<ChatMessage> { new UserChatMessage(message) },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Value.Content[0].Text;
    }

    /// <summary>Sends a multi-turn conversation and returns the assistant's reply text.</summary>
    /// <param name="messages">The conversation messages.</param>
    /// <param name="model">Model override, or null to use the default chat model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<string> ChatAsync(
        IEnumerable<ChatMessage> messages,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var chatClient = GetChatClient(model);
        var response = await chatClient.CompleteChatAsync(messages.ToList(), cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Value.Content[0].Text;
    }

    /// <summary>Streams a chat completion, yielding text deltas as they arrive.</summary>
    /// <param name="message">The user message to send.</param>
    /// <param name="model">Model override, or null to use the default chat model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async IAsyncEnumerable<string> StreamChatAsync(
        string message,
        string? model = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatClient = GetChatClient(model);
        await foreach (var update in chatClient.CompleteChatStreamingAsync(
            new List<ChatMessage> { new UserChatMessage(message) }).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            foreach (var part in update.ContentUpdate)
                yield return part.Text;
        }
    }

    #endregion

    #region Embeddings

    /// <summary>Gets an EmbeddingClient for the specified model.</summary>
    public EmbeddingClient GetEmbeddingClient(string? model = null) =>
        _client.GetEmbeddingClient(model ?? _options.DefaultEmbeddingModel);

    /// <summary>Returns the embedding vector for a single string.</summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="model">Model override, or null to use the default embedding model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<float[]> EmbedAsync(
        string text,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var embeddingClient = GetEmbeddingClient(model);
        var response = await embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Value.ToFloats().ToArray();
    }

    /// <summary>Returns embedding vectors for a batch of strings.</summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="model">Model override, or null to use the default embedding model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<float[][]> EmbedBatchAsync(
        IEnumerable<string> texts,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var embeddingClient = GetEmbeddingClient(model);
        var response = await embeddingClient.GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Value.Select(e => e.ToFloats().ToArray()).ToArray();
    }

    #endregion

    #region Image Generation

    /// <summary>Gets an ImageClient for the specified model.</summary>
    public ImageClient GetImageClient(string? model = null) =>
        _client.GetImageClient(model ?? _options.DefaultImageModel);

    /// <summary>Generates an image from a text prompt and returns the URL.</summary>
    /// <param name="prompt">The image description.</param>
    /// <param name="size">Image size: "1024x1024" (default), "1792x1024", or "1024x1792".</param>
    /// <param name="model">Model override, or null to use the default image model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<string> GenerateImageAsync(
        string prompt,
        string size = "1024x1024",
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var imageClient = GetImageClient(model);
        var options = new ImageGenerationOptions
        {
            Size = size switch
            {
                "1792x1024" => GeneratedImageSize.W1792xH1024,
                "1024x1792" => GeneratedImageSize.W1024xH1792,
                _ => GeneratedImageSize.W1024xH1024,
            },
        };
        var response = await imageClient.GenerateImageAsync(prompt, options, cancellationToken).ConfigureAwait(false);
        return response.Value.ImageUri?.ToString()
            ?? throw new InvalidOperationException("Image generation did not return a URL.");
    }

    /// <summary>Generates multiple images from a text prompt and returns their URLs.</summary>
    /// <param name="prompt">The image description.</param>
    /// <param name="count">Number of images (1–10).</param>
    /// <param name="size">Image size: "1024x1024" (default), "1792x1024", or "1024x1792".</param>
    /// <param name="model">Model override, or null to use the default image model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<string[]> GenerateImagesAsync(
        string prompt,
        int count = 1,
        string size = "1024x1024",
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var imageClient = GetImageClient(model);
        var options = new ImageGenerationOptions
        {
            Size = size switch
            {
                "1792x1024" => GeneratedImageSize.W1792xH1024,
                "1024x1792" => GeneratedImageSize.W1024xH1792,
                _ => GeneratedImageSize.W1024xH1024,
            },
        };
        var response = await imageClient.GenerateImagesAsync(prompt, count, options, cancellationToken).ConfigureAwait(false);
        return response.Value
            .Select(img => img.ImageUri?.ToString() ?? string.Empty)
            .Where(url => !string.IsNullOrEmpty(url))
            .ToArray();
    }

    #endregion
}
