using System.Net;
using Markdig;

namespace FRELODY.Docs.Services;

public class DocumentService
{
    private readonly HttpClient _http;
    private readonly MarkdownPipeline _pipeline;
    private readonly Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    public DocumentService(HttpClient http)
    {
        _http = http;
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

        if (_cache.TryGetValue(slug, out var cachedHtml))
        {
            return new DocumentLoadResult(cachedHtml, true);
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
            return new DocumentLoadResult(html, true);
        }
        catch (HttpRequestException)
        {
            return new DocumentLoadResult(string.Empty, false);
        }
    }
}

public record DocumentLoadResult(string Html, bool Found);
