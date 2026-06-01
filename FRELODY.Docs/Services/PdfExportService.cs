using Microsoft.JSInterop;
using FRELODY.Docs.Models;

namespace FRELODY.Docs.Services;

/// <summary>
/// On-demand PDF export for the docs site.
/// Per-page export reads the live <c>.doc-article</c> DOM (no re-fetch).
/// Full-guide export walks the navigation tree, fetches every page the current
/// audience is allowed to see, assembles a cover + table of contents + sections,
/// and hands the whole thing to the browser's native print dialog.
/// </summary>
public class PdfExportService
{
    private readonly IJSRuntime _js;
    private readonly DocumentService _docs;
    private readonly NavigationDataService _nav;
    private readonly AuthService _auth;

    private CancellationTokenSource? _cts;

    public PdfExportService(IJSRuntime js, DocumentService docs, NavigationDataService nav, AuthService auth)
    {
        _js = js;
        _docs = docs;
        _nav = nav;
        _auth = auth;
    }

    /// <summary>Raised whenever export progress changes (or when an export ends).</summary>
    public event Action<PdfExportProgress>? OnProgress;

    public bool IsRunning { get; private set; }

    /// <summary>Cancel a running full-guide export. No-op if nothing is running.</summary>
    public void Cancel() => _cts?.Cancel();

    /// <summary>
    /// Make sure the pdf-export.js helper is loaded. If a stale index.html
    /// shipped without the &lt;script&gt; tag (cached browser, dev-server
    /// hot-reload edge case), inject it on demand and wait for it to define
    /// <c>window.frelodyPdf</c>.
    /// </summary>
    private async Task EnsureJsLoadedAsync()
    {
        const string ensure = @"
(function () {
    if (window.frelodyPdf) return Promise.resolve(true);
    if (window.__frelodyPdfLoading) return window.__frelodyPdfLoading;
    window.__frelodyPdfLoading = new Promise(function (resolve) {
        var s = document.createElement('script');
        s.src = 'js/pdf-export.js?v=' + Date.now();
        s.onload = function () { resolve(!!window.frelodyPdf); };
        s.onerror = function () { resolve(false); };
        document.head.appendChild(s);
    });
    return window.__frelodyPdfLoading;
})()";
        try
        {
            await _js.InvokeVoidAsync("eval", ensure);
            // Poll briefly until the script registers the global.
            for (var i = 0; i < 30; i++)
            {
                var ok = await _js.InvokeAsync<bool>("eval", "!!window.frelodyPdf");
                if (ok) return;
                await Task.Delay(100);
            }
        }
        catch
        {
            // Swallow — the subsequent invoke will surface a clearer error if
            // the helper truly cannot be loaded.
        }
    }

    /// <summary>
    /// Print the currently visible documentation page. Re-fetches the page's
    /// markdown so the PDF includes images regardless of whether the live DOM
    /// stripped any 404'd <c>&lt;img&gt;</c> tags via <c>onerror</c>.
    /// </summary>
    public async Task ExportCurrentPageAsync(string slug, string pageTitle)
    {
        if (IsRunning) return;
        IsRunning = true;
        try
        {
            Report(new PdfExportProgress(true, "Preparing PDF…", 0, 1));
            await EnsureJsLoadedAsync();

            var fetched = await _docs.LoadAsync(slug);
            var article = fetched.Found && !string.IsNullOrWhiteSpace(fetched.Html)
                ? fetched.Html
                : await _js.InvokeAsync<string>("frelodyPdf.getArticleHtml");
            if (string.IsNullOrWhiteSpace(article))
            {
                article = "<p><em>This page has no content yet.</em></p>";
            }

            var safeTitle = string.IsNullOrWhiteSpace(pageTitle) ? "FRELODY Documentation" : pageTitle;
            var body =
                $"<article class=\"pdf-page\">" +
                $"  <div class=\"pdf-page-header\">FRELODY Documentation</div>" +
                $"  <h1>{System.Net.WebUtility.HtmlEncode(safeTitle)}</h1>" +
                $"  {article}" +
                $"</article>";

            Report(new PdfExportProgress(true, "Opening print dialog…", 1, 1));

            await _js.InvokeVoidAsync("frelodyPdf.printDocument", new { title = safeTitle, bodyHtml = body });
        }
        finally
        {
            IsRunning = false;
            Report(PdfExportProgress.Idle);
        }
    }

    /// <summary>
    /// Build a single PDF with cover, table of contents and every leaf page the
    /// current user is allowed to see. Cancellable from the overlay UI.
    /// </summary>
    public async Task ExportFullGuideAsync()
    {
        if (IsRunning) return;
        IsRunning = true;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            await EnsureJsLoadedAsync();

            var current = _auth.CurrentAudience;
            var sections = new List<(NavItem section, List<NavItem> leaves)>();
            foreach (var section in _nav.Sections)
            {
                if (section.EffectiveAudience > current) continue;
                var leaves = new List<NavItem>();
                CollectVisibleLeaves(section, current, leaves);
                if (leaves.Count > 0) sections.Add((section, leaves));
            }

            var totalLeaves = sections.Sum(s => s.leaves.Count);
            if (totalLeaves == 0)
            {
                Report(new PdfExportProgress(true, "Nothing to export.", 0, 0));
                await Task.Delay(800, ct);
                return;
            }

            Report(new PdfExportProgress(true, $"Fetching page 0 of {totalLeaves}…", 0, totalLeaves));

            var fetched = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int done = 0;
            const int BatchSize = 6;

            var allLeaves = sections.SelectMany(s => s.leaves).ToList();
            for (int i = 0; i < allLeaves.Count; i += BatchSize)
            {
                ct.ThrowIfCancellationRequested();
                var batch = allLeaves.Skip(i).Take(BatchSize).ToList();
                var tasks = batch.Select(async leaf =>
                {
                    var r = await _docs.LoadAsync(leaf.Slug, ct);
                    return (leaf.Slug, r.Found ? r.Html : "<p><em>This page has no content yet.</em></p>");
                });
                var results = await Task.WhenAll(tasks);
                foreach (var (slug, html) in results)
                {
                    fetched[slug] = html;
                }
                done = Math.Min(done + batch.Count, totalLeaves);
                Report(new PdfExportProgress(true, $"Fetching page {done} of {totalLeaves}…", done, totalLeaves));
            }

            ct.ThrowIfCancellationRequested();
            Report(new PdfExportProgress(true, "Assembling PDF…", totalLeaves, totalLeaves));

            var sb = new System.Text.StringBuilder();
            var generated = DateTime.Now.ToString("dd MMMM yyyy");
            var audienceLabel = current switch
            {
                Audience.Admin => "Administrator edition (all sections)",
                Audience.Premium => "Premium edition",
                Audience.Member => "Signed-in edition",
                _ => "Public edition",
            };

            sb.Append("<section class=\"pdf-cover\">");
            sb.Append("<h1>FRELODY Documentation</h1>");
            sb.Append("<p class=\"pdf-cover-sub\">Paste a song link. Get the chords. Slow it down and loop it.</p>");
            sb.Append($"<p class=\"pdf-cover-sub\">{System.Net.WebUtility.HtmlEncode(audienceLabel)}</p>");
            sb.Append($"<p class=\"pdf-cover-meta\">Generated {generated} · {totalLeaves} pages · FRELODY</p>");
            sb.Append("</section>");

            sb.Append("<nav class=\"pdf-toc\"><h2>Table of contents</h2><ul>");
            foreach (var (section, leaves) in sections)
            {
                sb.Append($"<li class=\"toc-section\">{System.Net.WebUtility.HtmlEncode(section.Title)}</li>");
                foreach (var leaf in leaves)
                {
                    var anchor = SlugToAnchor(leaf.Slug);
                    sb.Append($"<li class=\"toc-page\"><a href=\"#{anchor}\">{System.Net.WebUtility.HtmlEncode(leaf.Title)}</a></li>");
                }
            }
            sb.Append("</ul></nav>");

            foreach (var (section, leaves) in sections)
            {
                foreach (var leaf in leaves)
                {
                    var anchor = SlugToAnchor(leaf.Slug);
                    var html = fetched.TryGetValue(leaf.Slug, out var h) ? h : string.Empty;
                    sb.Append($"<article class=\"pdf-page\" id=\"{anchor}\">");
                    sb.Append($"<div class=\"pdf-page-header\">{System.Net.WebUtility.HtmlEncode(section.Title)}</div>");
                    sb.Append(html);
                    sb.Append("</article>");
                }
            }

            Report(new PdfExportProgress(true, "Opening print dialog…", totalLeaves, totalLeaves));
            await _js.InvokeVoidAsync("frelodyPdf.printDocument", new
            {
                title = "FRELODY Documentation",
                bodyHtml = sb.ToString()
            });
        }
        catch (OperationCanceledException)
        {
            Report(new PdfExportProgress(true, "Cancelled.", 0, 0));
            await Task.Delay(400);
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
            Report(PdfExportProgress.Idle);
        }
    }

    private void Report(PdfExportProgress p) => OnProgress?.Invoke(p);

    private static void CollectVisibleLeaves(NavItem node, Audience current, List<NavItem> leaves)
    {
        if (!node.HasChildren)
        {
            if (!string.IsNullOrEmpty(node.Slug) && node.Audience <= current)
            {
                leaves.Add(node);
            }
            return;
        }
        foreach (var c in node.Children)
        {
            if (c.EffectiveAudience > current) continue;
            CollectVisibleLeaves(c, current, leaves);
        }
    }

    private static string SlugToAnchor(string slug)
        => "p-" + new string(slug.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
}

public sealed record PdfExportProgress(bool IsActive, string Status, int Done, int Total)
{
    public static PdfExportProgress Idle { get; } = new(false, string.Empty, 0, 0);

    public int Percent => Total <= 0 ? 0 : (int)Math.Round(100.0 * Done / Total);
}
