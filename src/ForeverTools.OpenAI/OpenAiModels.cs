namespace ForeverTools.OpenAI;

/// <summary>Well-known OpenAI model identifiers.</summary>
public static class OpenAiModels
{
    /// <summary>Chat completion models.</summary>
    public static class Chat
    {
        public const string Gpt4o = "gpt-4o";
        public const string Gpt4oMini = "gpt-4o-mini";
        public const string Gpt4Turbo = "gpt-4-turbo";
        public const string Gpt4 = "gpt-4";
        public const string Gpt35Turbo = "gpt-3.5-turbo";
        public const string O1 = "o1";
        public const string O1Mini = "o1-mini";
        public const string O3Mini = "o3-mini";
    }

    /// <summary>Image generation models.</summary>
    public static class Image
    {
        public const string DallE3 = "dall-e-3";
        public const string DallE2 = "dall-e-2";
    }

    /// <summary>Embedding models.</summary>
    public static class Embedding
    {
        public const string TextEmbedding3Small = "text-embedding-3-small";
        public const string TextEmbedding3Large = "text-embedding-3-large";
        public const string TextEmbeddingAda002 = "text-embedding-ada-002";
    }
}
