using HtmlAgilityPack;

namespace ForeverTools.Harvest;

/// <summary>
/// Result from a Harvest pipeline fetch.
/// </summary>
public class HarvestResult
{
    /// <summary>The fetched URL.</summary>
    public required string Url { get; init; }

    /// <summary>HTTP status code returned by the server.</summary>
    public int StatusCode { get; init; }

    /// <summary>Raw HTML body.</summary>
    public string? Html { get; init; }

    /// <summary>Parsed HTML document (lazy-loaded from Html).</summary>
    public HtmlDocument? Document => _doc ??= ParseDocument();

    private HtmlDocument? _doc;

    /// <summary>Whether a captcha was detected and solved during this request.</summary>
    public bool CaptchaSolved { get; init; }

    /// <summary>Which pipeline stage delivered this result (direct/scraperapi/proxy).</summary>
    public string Stage { get; init; } = "unknown";

    /// <summary>True when StatusCode is 2xx and Html is not empty.</summary>
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300 && !string.IsNullOrWhiteSpace(Html);

    /// <summary>
    /// Select text content from the first matching CSS-like XPath selector.
    /// Supports: tag, .class, #id, tag.class combinations.
    /// </summary>
    public string? SelectText(string selector)
    {
        return Document?.DocumentNode.SelectSingleNode(SelectorToXPath(selector))?.InnerText.Trim();
    }

    /// <summary>Select inner HTML of the first matching element.</summary>
    public string? SelectHtml(string selector)
    {
        return Document?.DocumentNode.SelectSingleNode(SelectorToXPath(selector))?.InnerHtml.Trim();
    }

    /// <summary>Select all matching elements and return their text content.</summary>
    public IEnumerable<string> SelectAll(string selector)
    {
        if (Document == null) yield break;
        var nodes = Document.DocumentNode.SelectNodes(SelectorToXPath(selector));
        if (nodes == null) yield break;
        foreach (var node in nodes)
            yield return node.InnerText.Trim();
    }

    private HtmlDocument? ParseDocument()
    {
        if (string.IsNullOrWhiteSpace(Html)) return null;
        var doc = new HtmlDocument();
        doc.LoadHtml(Html);
        return doc;
    }

    private static string SelectorToXPath(string selector)
    {
        // Minimal CSS→XPath: handles tag, .class, #id, tag.class
        if (selector.StartsWith('#'))
            return $"//*[@id='{selector[1..]}']";
        if (selector.StartsWith('.'))
            return $"//*[contains(@class,'{selector[1..]}')]";
        if (selector.Contains('.'))
        {
            var parts = selector.Split('.', 2);
            return $"//{parts[0]}[contains(@class,'{parts[1]}')]";
        }
        return $"//{selector}";
    }
}
