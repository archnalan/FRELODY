using System;
using System.Linq;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using FRELODYSHRD.Dtos;

namespace FRELODYAPIs.Services.WebSong
{
    /// <summary>
    /// Generic source that works for any page placing its chord chart inside one or more
    /// monospace &lt;pre&gt; blocks (the universal "chords-over-lyrics" convention).
    /// Used as the fallback when no host-specific strategy claims the URL.
    /// </summary>
    public sealed class MonospacePreBlockSource : IWebSongSource
    {
        // Generic source runs last.
        public int Priority => int.MaxValue;

        public bool CanHandle(Uri url) => true;

        public WebSongFetchResult Extract(Uri url, IDocument document)
        {
            var preText = ExtractPreBlocks(document);
            var title = ExtractTitle(document);

            return new WebSongFetchResult
            {
                SourceUrl = url.ToString(),
                SourceHost = url.Host,
                Title = title,
                SongNumber = null,
                PreBlockText = preText,
                IsSupported = !string.IsNullOrWhiteSpace(preText),
                Message = string.IsNullOrWhiteSpace(preText)
                    ? "No <pre> block with chord/lyric content was found."
                    : null
            };
        }

        /// <summary>
        /// Walks each &lt;pre&gt; element, replacing &lt;br&gt; with '\n' and concatenating the
        /// raw text content. Preserves whitespace columns, decodes HTML entities (handled by
        /// AngleSharp's <c>TextContent</c>), and normalizes &amp;nbsp; to regular spaces.
        /// </summary>
        public static string ExtractPreBlocks(IDocument document)
        {
            var preElements = document.QuerySelectorAll("pre").ToArray();
            if (preElements.Length == 0) return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < preElements.Length; i++)
            {
                var text = ConvertPreToText(preElements[i]);
                if (string.IsNullOrWhiteSpace(text)) continue;

                if (sb.Length > 0) sb.Append('\n');
                sb.Append(text);
            }

            return sb.ToString().Replace("\u00A0", " ").Replace("\r\n", "\n").Replace("\r", "\n");
        }

        /// <summary>
        /// Faithfully reproduces a &lt;pre&gt; block as plain text:
        ///  - &lt;br&gt; → '\n'
        ///  - inline tags like &lt;font&gt;/&lt;span&gt;/&lt;b&gt; contribute only their text
        ///  - text nodes preserved verbatim (whitespace intact)
        /// </summary>
        private static string ConvertPreToText(IElement pre)
        {
            var sb = new StringBuilder();
            Walk(pre, sb);
            return sb.ToString();
        }

        private static void Walk(INode node, StringBuilder sb)
        {
            foreach (var child in node.ChildNodes)
            {
                switch (child.NodeType)
                {
                    case NodeType.Text:
                        sb.Append(child.TextContent);
                        break;

                    case NodeType.Element:
                        var el = (IElement)child;
                        if (el is IHtmlBreakRowElement)
                        {
                            sb.Append('\n');
                        }
                        else
                        {
                            // Recurse so inline color/style spans don't drop their text.
                            Walk(el, sb);
                        }
                        break;
                }
            }
        }

        public static string? ExtractTitle(IDocument document)
        {
            // Prefer the first heading inside the body; fall back to <title>.
            var heading = document.QuerySelector("h1, h2");
            var title = heading?.TextContent?.Trim();
            if (!string.IsNullOrWhiteSpace(title)) return title;

            var docTitle = document.Title?.Trim();
            return string.IsNullOrWhiteSpace(docTitle) ? null : docTitle;
        }
    }
}
