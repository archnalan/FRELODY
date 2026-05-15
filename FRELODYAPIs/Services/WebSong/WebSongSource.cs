using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using FRELODYSHRD.Dtos;
using Microsoft.AspNetCore.WebUtilities;

namespace FRELODYAPIs.Services.WebSong
{
    /// <summary>
    /// Single web-song extraction service. Internally dispatches per host to the appropriate
    /// extraction strategy and falls back to a generic monospace &lt;pre&gt; block scraper for
    /// any unknown host. Adding support for a new site = one private extractor + one
    /// dispatch entry in <see cref="ExtractInternal"/>.
    /// </summary>
    public sealed class WebSongSource : IWebSongSource
    {
        // Single registered source — priority is irrelevant but kept for the interface.
        public int Priority => 0;

        // Always claims the URL; per-host dispatch happens inside Extract.
        public bool CanHandle(Uri url) => true;

        public WebSongFetchResult Extract(Uri url, IDocument document)
            => ExtractInternal(url, document);

        // ──────────────────────────────────────────────────────────────────────────
        //  Dispatch
        // ──────────────────────────────────────────────────────────────────────────

        private static WebSongFetchResult ExtractInternal(Uri url, IDocument document)
        {
            var host = url.Host;

            if (host.EndsWith("bradwarden.com", StringComparison.OrdinalIgnoreCase) &&
                url.AbsolutePath.StartsWith("/music/hymnchords", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractBradWarden(url, document);
            }

            if (host.EndsWith("worshiptogether.com", StringComparison.OrdinalIgnoreCase) &&
                url.AbsolutePath.StartsWith("/songs/", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractWorshipTogether(url, document);
            }

            return ExtractGenericPreBlocks(url, document);
        }

        // ──────────────────────────────────────────────────────────────────────────
        //  Strategy: bradwarden.com — chord chart inside a single <pre>, heading
        //  "N. Title" gives the song number + title.
        // ──────────────────────────────────────────────────────────────────────────

        private static readonly Regex BradWardenHeadingRegex = new(
            @"^\s*(?<num>\d+)\s*\.\s*(?<title>.+?)\s*$",
            RegexOptions.Compiled);

        private static WebSongFetchResult ExtractBradWarden(Uri url, IDocument document)
        {
            var preText = ExtractPreBlocks(document);

            string? title = null;
            int? songNumber = null;

            var heading = document.QuerySelector("h2")?.TextContent?.Trim();
            if (!string.IsNullOrEmpty(heading))
            {
                var match = BradWardenHeadingRegex.Match(heading);
                if (match.Success)
                {
                    if (int.TryParse(match.Groups["num"].Value, out var n)) songNumber = n;
                    title = match.Groups["title"].Value.Trim();
                }
                else
                {
                    title = heading;
                }
            }

            if (songNumber is null)
            {
                var query = QueryHelpers.ParseQuery(url.Query);
                if (query.TryGetValue("num", out var raw) && int.TryParse(raw, out var n))
                    songNumber = n;
            }

            return Result(url, title, songNumber, preText,
                "No <pre> chord block was found on this page.");
        }

        // ──────────────────────────────────────────────────────────────────────────
        //  Strategy: worshiptogether.com — structured ChordPro DOM
        //  (.chord-pro-line > .chord-pro-segment > {.chord-pro-note, .chord-pro-lyric}).
        //  Synthesized into column-aligned text so the existing parser handles it.
        // ──────────────────────────────────────────────────────────────────────────

        private static WebSongFetchResult ExtractWorshipTogether(Uri url, IDocument document)
        {
            var preText = SynthesizeChordProBlock(document);
            var title = ExtractWorshipTogetherTitle(document);

            return Result(url, title, null, preText,
                "No chord-pro content was found on this Worship Together page.");
        }

        public static string SynthesizeChordProBlock(IDocument document)
        {
            var lines = document.QuerySelectorAll(".chord-pro-line").ToArray();
            if (lines.Length == 0) return string.Empty;

            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                var segments = line.QuerySelectorAll(".chord-pro-segment").ToArray();
                if (segments.Length == 0) { sb.Append('\n'); continue; }

                var pairs = segments
                    .Select(s => (
                        Chord: (s.QuerySelector(".chord-pro-note")?.TextContent ?? string.Empty)
                                .Replace("\u00A0", " ").Trim(),
                        Lyric: (s.QuerySelector(".chord-pro-lyric")?.TextContent ?? string.Empty)
                                .Replace("\u00A0", " ")
                    ))
                    .ToArray();

                bool hasAnyChord = pairs.Any(p => !string.IsNullOrEmpty(p.Chord));

                if (!hasAnyChord)
                {
                    var text = string.Concat(pairs.Select(p => p.Lyric)).Trim();
                    if (string.IsNullOrEmpty(text)) { sb.Append('\n'); continue; }

                    if (LooksLikeSectionLabel(text) && !text.StartsWith("["))
                        sb.Append('[').Append(text).Append(']');
                    else
                        sb.Append(text);
                    sb.Append('\n');
                    continue;
                }

                var chordRow = new StringBuilder();
                var lyricRow = new StringBuilder();
                foreach (var p in pairs)
                {
                    int startCol = lyricRow.Length;
                    if (chordRow.Length < startCol)
                        chordRow.Append(' ', startCol - chordRow.Length);

                    chordRow.Append(p.Chord);

                    var lyric = p.Lyric;
                    int needed = p.Chord.Length + (p.Chord.Length > 0 ? 1 : 0);
                    if (lyric.Length < needed)
                        lyric = lyric.PadRight(needed);
                    lyricRow.Append(lyric);
                }

                sb.Append(chordRow.ToString().TrimEnd()).Append('\n');
                sb.Append(lyricRow.ToString().TrimEnd()).Append('\n');
            }

            return sb.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static bool LooksLikeSectionLabel(string text)
        {
            if (text.Length > 30) return false;
            var lower = text.ToLowerInvariant();
            return lower.StartsWith("verse") || lower.StartsWith("chorus")
                || lower.StartsWith("bridge") || lower.StartsWith("pre-chorus")
                || lower.StartsWith("prechorus") || lower.StartsWith("intro")
                || lower.StartsWith("outro") || lower.StartsWith("tag")
                || lower.StartsWith("interlude") || lower.StartsWith("refrain")
                || lower.StartsWith("ending") || lower.StartsWith("coda");
        }

        private static string? ExtractWorshipTogetherTitle(IDocument document)
        {
            var heading = document.QuerySelector("h1.t-song-details__marquee__headline")?.TextContent?.Trim()
                       ?? document.QuerySelector("h1")?.TextContent?.Trim();
            if (!string.IsNullOrWhiteSpace(heading)) return heading;

            var docTitle = document.Title?.Trim();
            if (string.IsNullOrWhiteSpace(docTitle)) return null;

            var pipe = docTitle.IndexOf('|');
            if (pipe > 0) docTitle = docTitle.Substring(0, pipe).Trim();
            const string suffix = "Lyrics and Chords";
            if (docTitle.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                docTitle = docTitle.Substring(0, docTitle.Length - suffix.Length).Trim();
            return string.IsNullOrWhiteSpace(docTitle) ? null : docTitle;
        }

        // ──────────────────────────────────────────────────────────────────────────
        //  Strategy: generic — any page with monospace <pre> chord blocks.
        // ──────────────────────────────────────────────────────────────────────────

        private static WebSongFetchResult ExtractGenericPreBlocks(Uri url, IDocument document)
        {
            var preText = ExtractPreBlocks(document);
            var title = ExtractGenericTitle(document);

            return Result(url, title, null, preText,
                "No <pre> block with chord/lyric content was found.");
        }

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
                            sb.Append('\n');
                        else
                            Walk(el, sb);
                        break;
                }
            }
        }

        private static string? ExtractGenericTitle(IDocument document)
        {
            var heading = document.QuerySelector("h1, h2");
            var title = heading?.TextContent?.Trim();
            if (!string.IsNullOrWhiteSpace(title)) return title;

            var docTitle = document.Title?.Trim();
            return string.IsNullOrWhiteSpace(docTitle) ? null : docTitle;
        }

        // ──────────────────────────────────────────────────────────────────────────
        //  Shared
        // ──────────────────────────────────────────────────────────────────────────

        private static WebSongFetchResult Result(
            Uri url, string? title, int? songNumber, string preText, string emptyMessage)
            => new()
            {
                SourceUrl = url.ToString(),
                SourceHost = url.Host,
                Title = title,
                SongNumber = songNumber,
                PreBlockText = preText,
                IsSupported = !string.IsNullOrWhiteSpace(preText),
                Message = string.IsNullOrWhiteSpace(preText) ? emptyMessage : null
            };
    }
}
