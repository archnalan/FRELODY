using System.Net;
using System.Text.RegularExpressions;
using FRELODY.Docs.Models;
using Markdig;

namespace FRELODY.Docs.Services;

public class DocumentService
{
    private readonly HttpClient _http;
    private readonly DocMediaService _media;
    private readonly MarkdownPipeline _pipeline;

    // Caches the rendered-but-not-yet-injected HTML per slug. Media injection runs fresh on every
    // load (it is cheap string work and depends on the live manifest), so a manifest change is
    // reflected immediately without invalidating this cache.
    private readonly Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    public DocumentService(HttpClient http, DocMediaService media)
    {
        _http = http;
        _media = media;
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoLinks()
            .UsePipeTables()
            .UseEmphasisExtras()
            .UseGenericAttributes()
            .UseTaskLists()
            .UseAutoIdentifiers()
            .Build();
    }

    public async Task<DocumentLoadResult> LoadAsync(string slug, CancellationToken ct = default)
    {
        slug = (slug ?? string.Empty).Trim('/');
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "index";
        }

        await _media.EnsureLoadedAsync();

        if (_cache.TryGetValue(slug, out var cachedHtml))
        {
            return new DocumentLoadResult(InjectMedia(slug, cachedHtml), true);
        }

        try
        {
            var path = $"content/{slug}.md";
            using var resp = await _http.GetAsync(path, ct);
            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                return new DocumentLoadResult(string.Empty, false);
            }
            resp.EnsureSuccessStatusCode();
            var md = await resp.Content.ReadAsStringAsync(ct);
            var html = Markdown.ToHtml(md, _pipeline);
            _cache[slug] = html;
            return new DocumentLoadResult(InjectMedia(slug, html), true);
        }
        catch (HttpRequestException)
        {
            return new DocumentLoadResult(string.Empty, false);
        }
    }

    /// <summary>
    /// Replaces the <c>data-media-slot</c> placeholders on this page with the real image/video from
    /// the manifest. Only slots belonging to this page are considered. Unset slots keep their
    /// placeholder markup.
    /// </summary>
    private string InjectMedia(string slug, string html)
    {
        var prefix = slug.Replace('/', '-') + "--";
        foreach (var kv in _media.Slots)
        {
            if (!kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var entry = kv.Value;
            if (entry.HasImage) html = ReplaceImage(html, kv.Key, entry);
            if (entry.HasVideo) html = ReplaceVideo(html, kv.Key, entry);
        }
        return html;
    }

    private string ReplaceImage(string html, string key, DocMediaEntry entry)
    {
        var pattern = $"<figure class=\"img-frame\" data-media-slot=\"{Regex.Escape(key)}\"[^>]*>[\\s\\S]*?</figure>";
        var slot = MediaRegistry.Find(key);
        var aspect = slot?.AspectRatio ?? "16 / 9";
        var alt = WebUtility.HtmlEncode(entry.Caption ?? slot?.Context ?? string.Empty);
        var src = WebUtility.HtmlEncode(_media.AbsoluteImageUrl(entry) ?? string.Empty);
        var figcaption = string.IsNullOrWhiteSpace(entry.Caption)
            ? string.Empty
            : $"<figcaption class=\"img-frame-figcaption\">{WebUtility.HtmlEncode(entry.Caption)}</figcaption>";
        var replacement =
            $"<figure class=\"img-frame\" data-media-slot=\"{key}\" style=\"aspect-ratio: {aspect};\">" +
            $"<img src=\"{src}\" alt=\"{alt}\" loading=\"lazy\" />{figcaption}</figure>";
        return Regex.Replace(html, pattern, _ => replacement);
    }

    private string ReplaceVideo(string html, string key, DocMediaEntry entry)
    {
        var pattern = $"<div class=\"video-embed\" data-media-slot=\"{Regex.Escape(key)}\"[^>]*>[\\s\\S]*?</div>";
        var slot = MediaRegistry.Find(key);
        var title = WebUtility.HtmlEncode(slot?.Context ?? "FRELODY walkthrough");
        var vid = WebUtility.HtmlEncode(entry.VideoId ?? string.Empty);
        var replacement =
            $"<div class=\"video-embed\" data-media-slot=\"{key}\">" +
            $"<iframe src=\"https://www.youtube-nocookie.com/embed/{vid}\" title=\"{title}\" loading=\"lazy\" " +
            $"allow=\"accelerator; clipboard-write; encrypted-media; picture-in-picture\" allowfullscreen></iframe></div>";
        return Regex.Replace(html, pattern, _ => replacement);
    }
}

public record DocumentLoadResult(string Html, bool Found);
