namespace ForeverTools.Harvest;

/// <summary>
/// Configuration for the Harvest scraping pipeline.
/// </summary>
public class HarvestOptions
{
    /// <summary>
    /// ScraperAPI key. When set, all fetches are routed via ScraperAPI for anti-bot bypass.
    /// Get your key: https://www.scraperapi.com/signup?fp_ref=chris88
    /// </summary>
    public string? ScraperApiKey { get; set; }

    /// <summary>
    /// BrightData customer ID. When set together with Zone + ProxyPassword, requests use
    /// residential proxies. Ignored when ScraperApiKey is set (ScraperAPI manages its own proxy).
    /// </summary>
    public string? ProxyCustomerId { get; set; }

    /// <summary>BrightData zone name.</summary>
    public string? ProxyZone { get; set; }

    /// <summary>BrightData zone password.</summary>
    public string? ProxyPassword { get; set; }

    /// <summary>
    /// Type of proxy to use when BrightData is configured (default: Residential).
    /// </summary>
    public ForeverTools.Proxy.ProxyType ProxyType { get; set; } = ForeverTools.Proxy.ProxyType.Residential;

    /// <summary>
    /// 2Captcha API key. When set, captcha challenges encountered during fetch are solved automatically.
    /// </summary>
    public string? TwoCaptchaKey { get; set; }

    /// <summary>Timeout for each fetch attempt (default: 30s).</summary>
    public TimeSpan FetchTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Maximum retries on transient failures (default: 2).</summary>
    public int MaxRetries { get; set; } = 2;
}
