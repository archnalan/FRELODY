using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FRELODYUI.Shared.Pages.Discover;

// Renders a clean, user-facing message for an analysis failure. Critically: never
// dumps raw HTML into the UI — when the upstream chain (Cloudflare, Nginx, the
// provider) returns an HTML error page on a 504/5xx, the previous implementation
// pasted the whole document into the page subline. Here we detect HTML, map known
// upstream signals (YouTube bot wall, TikTok extraction failures), and otherwise
// translate by status code.
internal static class AnalysisErrorMessage
{
    public enum SourceKind { YouTube, TikTok }

    public static string FromException(Exception ex, SourceKind source) => ex switch
    {
        Refit.ApiException api => FromResponse(api.StatusCode, api.Content, source),
        TaskCanceledException => "The analysis took too long and was cancelled. Please try again.",
        HttpRequestException => "We couldn't reach the analysis service. Check your connection and try again.",
        _ => GenericForSource(source)
    };

    public static string FromResponse(HttpStatusCode? status, string? body, SourceKind source)
    {
        // 1. Our own structured 422 carries { "message": "..." } — trust it verbatim.
        var apiMessage = TryReadJsonMessage(body);
        if (!string.IsNullOrWhiteSpace(apiMessage)) return apiMessage!;

        // 2. Sniff known upstream signals before any HTML/status fallback.
        if (!string.IsNullOrEmpty(body))
        {
            if (BotWallRegex.IsMatch(body))
                return "YouTube is asking us to confirm we're not a bot. Please try again in a moment, or try a different video.";

            if (source == SourceKind.TikTok && TikTokFailureRegex.IsMatch(body))
                return "We couldn't extract this TikTok. It may be private, removed, age-restricted, or geo-blocked.";
        }

        // 3. Map by HTTP status (covers Cloudflare's 504 HTML page and friends).
        var code = status.HasValue ? (int)status.Value : 0;
        if (code == 504)
            return "The analysis is taking longer than our gateway allows. Please try again in a moment.";
        if (code is 502 or 503)
            return "The analysis service is temporarily unavailable. Please try again in a moment.";

        // 4. HTML fallback — refuse to render an upstream error page into the UI.
        if (LooksLikeHtml(body))
            return GenericForSource(source);

        // 5. Plain-text body, capped so a runaway dump still can't fill the page.
        return Truncate(body, 240) ?? GenericForSource(source);
    }

    private static string GenericForSource(SourceKind source) => source switch
    {
        SourceKind.YouTube => "We couldn't analyze this YouTube video. The source service returned an unexpected response — please try again.",
        SourceKind.TikTok => "We couldn't analyze this TikTok. The source service returned an unexpected response — please try again.",
        _ => "We couldn't complete the analysis. Please try again."
    };

    private static readonly Regex BotWallRegex = new(
        @"sign in to confirm.*?(not a bot|you are human)|confirm you('?re| are) not a bot|failed to extract any player response|HTTP Error 429",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TikTokFailureRegex = new(
        @"unable to extract|video unavailable|video has been removed|geo[- ]?restrict|age[- ]?restrict|this video is private",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static string? TryReadJsonMessage(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var trimmed = raw.TrimStart();
        if (trimmed.Length == 0 || (trimmed[0] != '{' && trimmed[0] != '[')) return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("message", out var msg) &&
                msg.ValueKind == JsonValueKind.String)
                return msg.GetString();
        }
        catch (JsonException) { }
        return null;
    }

    private static bool LooksLikeHtml(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var s = raw.TrimStart();
        if (s.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)) return true;
        if (s.StartsWith("<html", StringComparison.OrdinalIgnoreCase)) return true;
        // Cloudflare error pages sometimes lead with IE conditional comments.
        if (s.StartsWith("<!--") && s.Contains("<html", StringComparison.OrdinalIgnoreCase)) return true;
        return s.Contains("<title>", StringComparison.OrdinalIgnoreCase)
            && s.Contains("</title>", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Truncate(string? s, int max)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        return t.Length <= max ? t : t[..max] + "…";
    }
}
