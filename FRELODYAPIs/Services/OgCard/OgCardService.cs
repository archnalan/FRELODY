using System.Net;
using SkiaSharp;

namespace FRELODYAPIs.Services.OgCard;

public sealed class OgCardService : IOgCardService
{
    private const int Width = 1200;
    private const int Height = 630;
    private const string OutputFolder = "share-og";

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<OgCardService> _logger;

    public OgCardService(IWebHostEnvironment env, ILogger<OgCardService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<string?> RenderPngAsync(OgCardContent content, string shareToken, CancellationToken ct = default)
    {
        try
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var folder = Path.Combine(webRoot, OutputFolder);
            Directory.CreateDirectory(folder);

            var safeToken = MakeFileNameSafe(shareToken);
            var fileName = $"{safeToken}.png";
            var fullPath = Path.Combine(folder, fileName);

            using var bitmap = new SKBitmap(Width, Height);
            using (var canvas = new SKCanvas(bitmap))
            {
                DrawCard(canvas, content);
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 90);

            await using var fs = File.Create(fullPath);
            data.SaveTo(fs);

            return $"/{OutputFolder}/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to render OG PNG for token {Token}", shareToken);
            return null;
        }
    }

    public string BuildHeroHtml(OgCardContent content)
    {
        // Keep inline styles so the landing page renders without depending on app CSS.
        var kindLabel = content.Kind == OgCardKind.Playlist ? "Playlist" : "Song";
        var title = WebUtility.HtmlEncode(content.Title ?? string.Empty);
        var subtitle = WebUtility.HtmlEncode(content.Subtitle ?? string.Empty);
        var tagline = WebUtility.HtmlEncode(content.Tagline ?? string.Empty);
        var meta = WebUtility.HtmlEncode(content.Meta ?? string.Empty);

        return $"""
            <div class="og-hero" style="max-width:760px;margin:0 auto;padding:48px 32px;font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;">
              <div style="background:linear-gradient(135deg,#1a1f3a 0%,#2d1b4e 50%,#4a1f6b 100%);border-radius:24px;padding:48px 40px;color:#fff;box-shadow:0 20px 60px rgba(0,0,0,0.25);">
                <div style="display:flex;align-items:center;gap:10px;opacity:0.8;font-size:0.85rem;letter-spacing:0.12em;text-transform:uppercase;margin-bottom:20px;">
                  <span style="display:inline-block;width:32px;height:3px;background:#ff4081;border-radius:2px;"></span>
                  <span>Frelody · {kindLabel}</span>
                </div>
                <h1 style="font-size:2.6rem;line-height:1.15;margin:0 0 12px 0;font-weight:800;">{title}</h1>
                {(string.IsNullOrWhiteSpace(subtitle) ? "" : $"<p style=\"margin:0 0 8px 0;font-size:1.15rem;opacity:0.95;\">{subtitle}</p>")}
                {(string.IsNullOrWhiteSpace(tagline) ? "" : $"<p style=\"margin:0 0 16px 0;font-size:1rem;opacity:0.75;\">{tagline}</p>")}
                {(string.IsNullOrWhiteSpace(meta) ? "" : $"<div style=\"display:inline-block;margin-top:8px;padding:6px 14px;background:rgba(255,255,255,0.12);border-radius:999px;font-size:0.85rem;\">{meta}</div>")}
              </div>
            </div>
            """;
    }

    // ─── PNG drawing ────────────────────────────────────────────────────────
    private static void DrawCard(SKCanvas canvas, OgCardContent content)
    {
        // Background gradient (matches the hero HTML)
        using (var bgPaint = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(Width, Height),
                new[]
                {
                    new SKColor(0x1a, 0x1f, 0x3a),
                    new SKColor(0x2d, 0x1b, 0x4e),
                    new SKColor(0x4a, 0x1f, 0x6b)
                },
                new float[] { 0f, 0.5f, 1f },
                SKShaderTileMode.Clamp)
        })
        {
            canvas.DrawRect(new SKRect(0, 0, Width, Height), bgPaint);
        }

        // Decorative accent bar
        using (var accent = new SKPaint { Color = new SKColor(0xff, 0x40, 0x81), IsAntialias = true })
        {
            canvas.DrawRoundRect(new SKRect(80, 96, 80 + 72, 96 + 8), 4, 4, accent);
        }

        // Kind label ("FRELODY · SONG")
        var kindLabel = content.Kind == OgCardKind.Playlist ? "FRELODY · PLAYLIST" : "FRELODY · SONG";
        using (var labelPaint = new SKPaint
        {
            Color = new SKColor(0xff, 0xff, 0xff, 0xcc),
            IsAntialias = true,
            TextSize = 24,
            Typeface = LoadTypeface(SKFontStyleWeight.SemiBold)
        })
        {
            canvas.DrawText(kindLabel, 170, 118, labelPaint);
        }

        // Title (wrap across up to 2 lines, auto-shrink)
        DrawWrappedTitle(canvas, content.Title ?? string.Empty, x: 80, y: 200, maxWidth: Width - 160);

        // Subtitle
        if (!string.IsNullOrWhiteSpace(content.Subtitle))
        {
            using var p = new SKPaint
            {
                Color = new SKColor(0xff, 0xff, 0xff, 0xee),
                IsAntialias = true,
                TextSize = 34,
                Typeface = LoadTypeface(SKFontStyleWeight.Normal)
            };
            canvas.DrawText(Truncate(content.Subtitle!, 60), 80, 410, p);
        }

        // Tagline
        if (!string.IsNullOrWhiteSpace(content.Tagline))
        {
            using var p = new SKPaint
            {
                Color = new SKColor(0xff, 0xff, 0xff, 0xaa),
                IsAntialias = true,
                TextSize = 26,
                Typeface = LoadTypeface(SKFontStyleWeight.Normal)
            };
            canvas.DrawText(Truncate(content.Tagline!, 80), 80, 455, p);
        }

        // Meta pill
        if (!string.IsNullOrWhiteSpace(content.Meta))
        {
            var text = Truncate(content.Meta!, 40);
            using var metaFont = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                TextSize = 24,
                Typeface = LoadTypeface(SKFontStyleWeight.SemiBold)
            };
            var textWidth = metaFont.MeasureText(text);
            var pillRect = new SKRect(80, 530, 80 + textWidth + 48, 530 + 48);
            using (var pillPaint = new SKPaint { Color = new SKColor(0xff, 0xff, 0xff, 0x33), IsAntialias = true })
            {
                canvas.DrawRoundRect(pillRect, 24, 24, pillPaint);
            }
            canvas.DrawText(text, pillRect.Left + 24, pillRect.Top + 32, metaFont);
        }

        // frelody.app footer mark (right side)
        using (var footer = new SKPaint
        {
            Color = new SKColor(0xff, 0xff, 0xff, 0x88),
            IsAntialias = true,
            TextSize = 22,
            Typeface = LoadTypeface(SKFontStyleWeight.Normal),
            TextAlign = SKTextAlign.Right
        })
        {
            canvas.DrawText("frelody.app", Width - 80, Height - 60, footer);
        }
    }

    private static void DrawWrappedTitle(SKCanvas canvas, string title, float x, float y, float maxWidth)
    {
        if (string.IsNullOrWhiteSpace(title)) return;

        var sizes = new float[] { 84, 72, 60, 52, 46 };
        foreach (var size in sizes)
        {
            using var paint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                TextSize = size,
                Typeface = LoadTypeface(SKFontStyleWeight.ExtraBold)
            };

            var lines = WrapText(title, paint, maxWidth, maxLines: 2);
            var totalWidth = lines.Max(l => paint.MeasureText(l));
            if (lines.Count <= 2 && totalWidth <= maxWidth)
            {
                float lineY = y;
                foreach (var line in lines)
                {
                    canvas.DrawText(line, x, lineY, paint);
                    lineY += size * 1.1f;
                }
                return;
            }
        }

        // Fallback: truncate at smallest size
        using var fallback = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            TextSize = sizes[^1],
            Typeface = LoadTypeface(SKFontStyleWeight.ExtraBold)
        };
        canvas.DrawText(EllipsizeToWidth(title, fallback, maxWidth), x, y, fallback);
    }

    private static List<string> WrapText(string text, SKPaint paint, float maxWidth, int maxLines)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var w in words)
        {
            var candidate = current.Length == 0 ? w : current + " " + w;
            if (paint.MeasureText(candidate) <= maxWidth)
            {
                current.Clear();
                current.Append(candidate);
            }
            else
            {
                if (current.Length > 0) lines.Add(current.ToString());
                current.Clear();
                current.Append(w);
                if (lines.Count == maxLines) break;
            }
        }
        if (current.Length > 0) lines.Add(current.ToString());
        return lines;
    }

    private static string EllipsizeToWidth(string text, SKPaint paint, float maxWidth)
    {
        if (paint.MeasureText(text) <= maxWidth) return text;
        const string ell = "…";
        var len = text.Length;
        while (len > 1 && paint.MeasureText(text[..len] + ell) > maxWidth) len--;
        return text[..len] + ell;
    }

    private static SKTypeface LoadTypeface(SKFontStyleWeight weight) =>
        SKTypeface.FromFamilyName(
            null, // system default
            weight,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright) ?? SKTypeface.Default;

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..(max - 1)] + "…";

    private static string MakeFileNameSafe(string token)
    {
        // Share tokens are Base64Url which is already file-safe; still sanitize defensively.
        var invalid = Path.GetInvalidFileNameChars();
        Span<char> buffer = stackalloc char[token.Length];
        for (int i = 0; i < token.Length; i++)
            buffer[i] = Array.IndexOf(invalid, token[i]) >= 0 ? '_' : token[i];
        return new string(buffer);
    }
}
