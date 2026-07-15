using System.Net;
using ForeverTools.Captcha;
using ForeverTools.Proxy;
using ForeverTools.ScraperAPI;

namespace ForeverTools.Harvest;

/// <summary>
/// Opinionated scraping pipeline: proxy → fetch → captcha → parse.
/// Composes ForeverTools.Proxy (BrightData), ScraperAPI, and Captcha into one fluent call.
///
/// Pipeline order:
/// 1. If ScraperApiKey set → fetch via ScraperAPI (handles proxy + JS rendering internally)
/// 2. Else if BrightData creds set → direct fetch routed through residential proxy
/// 3. Else → plain HttpClient fetch
/// 4. Detect captcha challenge in response (Cloudflare, reCAPTCHA markers)
/// 5. If captcha detected and TwoCaptchaKey set → solve + retry with token
/// 6. Return HarvestResult with parsed HtmlAgilityPack document
/// </summary>
public class HarvestClient : IDisposable
{
    private readonly HarvestOptions _options;
    private readonly ScraperApiClient? _scraperApi;
    private readonly BrightDataClient? _proxy;
    private readonly CaptchaClient? _captcha;
    private readonly HttpClient _http;
    private bool _disposed;

    private static readonly string[] _captchaMarkers = [
        "g-recaptcha", "h-captcha", "cf-turnstile",
        "Checking if the site connection is secure",
        "Just a moment", "Enable JavaScript and cookies",
    ];

    /// <summary>
    /// Creates a Harvest client with explicit options.
    /// </summary>
    public HarvestClient(HarvestOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (!string.IsNullOrWhiteSpace(options.ScraperApiKey))
        {
            _scraperApi = new ScraperApiClient(options.ScraperApiKey);
        }
        else if (!string.IsNullOrWhiteSpace(options.ProxyCustomerId)
                 && !string.IsNullOrWhiteSpace(options.ProxyZone)
                 && !string.IsNullOrWhiteSpace(options.ProxyPassword))
        {
            _proxy = new BrightDataClient(options.ProxyCustomerId, options.ProxyZone, options.ProxyPassword);
            var webProxy = _proxy.GetCredentials(options.ProxyType).ToWebProxy();
            _http = new HttpClient(new HttpClientHandler { Proxy = webProxy, UseProxy = true })
                { Timeout = options.FetchTimeout };
        }

        _http ??= new HttpClient { Timeout = options.FetchTimeout };

        if (!string.IsNullOrWhiteSpace(options.TwoCaptchaKey))
            _captcha = new CaptchaClient(options.TwoCaptchaKey);
    }

    /// <summary>
    /// Convenience constructor for ScraperAPI-only usage (no proxy, no captcha).
    /// </summary>
    public static HarvestClient WithScraperApi(string apiKey) =>
        new(new HarvestOptions { ScraperApiKey = apiKey });

    /// <summary>
    /// Convenience constructor: ScraperAPI fetch + 2Captcha solving.
    /// </summary>
    public static HarvestClient WithScraperApiAndCaptcha(string scraperApiKey, string twoCaptchaKey) =>
        new(new HarvestOptions { ScraperApiKey = scraperApiKey, TwoCaptchaKey = twoCaptchaKey });

    /// <summary>
    /// Fetch a URL through the configured pipeline and return a parsed result.
    /// </summary>
    public async Task<HarvestResult> FetchAsync(string url, CancellationToken ct = default)
    {
        for (int attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            var (html, statusCode, stage) = await FetchRawAsync(url, ct);

            if (html != null && DetectsCaptcha(html) && _captcha != null)
            {
                // Try to solve reCAPTCHA v2 (most common on scraping targets)
                try
                {
                    var siteKey = ExtractSiteKey(html);
                    if (siteKey != null)
                    {
                        var result = await _captcha.SolveReCaptchaV2Async(url, siteKey, cancellationToken: ct);
                    var token = result.Solution;
                        html = await InjectTokenAndRefetchAsync(url, token, ct);
                        return new HarvestResult { Url = url, StatusCode = statusCode, Html = html, CaptchaSolved = true, Stage = stage };
                    }
                }
                catch
                {
                    // captcha solve failed — fall through and return raw
                }
            }

            return new HarvestResult { Url = url, StatusCode = statusCode, Html = html, Stage = stage };
        }

        return new HarvestResult { Url = url, StatusCode = 0, Html = null, Stage = "failed" };
    }

    private async Task<(string? html, int status, string stage)> FetchRawAsync(string url, CancellationToken ct)
    {
        if (_scraperApi != null)
        {
            var html = await _scraperApi.ScrapeAsync(url, ct);
            return (html, 200, "scraperapi");
        }

        var r = await _http.GetAsync(url, ct);
        return (await r.Content.ReadAsStringAsync(ct), (int)r.StatusCode,
            _proxy != null ? "proxy" : "direct");
    }

    private static bool DetectsCaptcha(string html) =>
        _captchaMarkers.Any(m => html.Contains(m, StringComparison.OrdinalIgnoreCase));

    private static string? ExtractSiteKey(string html)
    {
        const string marker = "data-sitekey=\"";
        var idx = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        idx += marker.Length;
        var end = html.IndexOf('"', idx);
        return end > idx ? html[idx..end] : null;
    }

    private async Task<string?> InjectTokenAndRefetchAsync(string url, string token, CancellationToken ct)
    {
        // POST the g-recaptcha-response token back to the same page
        var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("g-recaptcha-response", token),
        ]);
        var resp = await _http.PostAsync(url, form, ct);
        return await resp.Content.ReadAsStringAsync(ct);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _scraperApi?.Dispose();
        _proxy?.Dispose();
        // CaptchaClient does not implement IDisposable
        _http.Dispose();
    }
}
