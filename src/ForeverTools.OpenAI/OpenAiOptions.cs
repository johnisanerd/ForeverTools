namespace ForeverTools.OpenAI;

/// <summary>
/// Configuration options for the OpenAI client.
/// Get your API key at https://platform.openai.com/api-keys
/// </summary>
public class OpenAiOptions
{
    /// <summary>The configuration section name for binding from appsettings.json.</summary>
    public const string SectionName = "OpenAI";

    /// <summary>Your OpenAI API key. Get one at https://platform.openai.com/api-keys</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Optional organisation ID for multi-org accounts.</summary>
    public string? OrganisationId { get; set; }

    /// <summary>The default model for chat completions. Defaults to gpt-4o-mini.</summary>
    public string DefaultChatModel { get; set; } = OpenAiModels.Chat.Gpt4oMini;

    /// <summary>The default model for image generation. Defaults to dall-e-3.</summary>
    public string DefaultImageModel { get; set; } = OpenAiModels.Image.DallE3;

    /// <summary>The default model for embeddings. Defaults to text-embedding-3-small.</summary>
    public string DefaultEmbeddingModel { get; set; } = OpenAiModels.Embedding.TextEmbedding3Small;

    /// <summary>Optional HTTP timeout in seconds.</summary>
    public int? TimeoutSeconds { get; set; }
}
