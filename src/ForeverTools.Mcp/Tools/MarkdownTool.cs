using System.ComponentModel;
using Markdig;
using ModelContextProtocol.Server;

namespace ForeverTools.Mcp.Tools;

[McpServerToolType]
public sealed class MarkdownTool
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    [McpServerTool, Description(
        "Convert Markdown text to HTML. Uses the Markdig library with advanced extensions " +
        "(tables, task lists, autolinks, definition lists, footnotes, and more). " +
        "No API key required — pure .NET processing.")]
    public string ConvertMarkdownToHtml(
        [Description("The Markdown text to convert")] string markdown,
        [Description("Wrap output in a minimal HTML document with <html><head><body> tags (default false — returns fragment only)")] bool fullDocument = false)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        var html = Markdown.ToHtml(markdown, Pipeline);

        if (!fullDocument)
            return html;

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body>
            {html}
            </body>
            </html>
            """;
    }
}
