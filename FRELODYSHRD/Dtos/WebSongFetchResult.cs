using System.Collections.Generic;

namespace FRELODYSHRD.Dtos
{
    /// <summary>
    /// Result of a server-side fetch from a supported chord/lyric web page.
    /// The server resolves the URL, parses the HTML, and returns the
    /// monospace "pre" block text plus metadata. The client then runs the
    /// column-aligned chord/lyric parser on <see cref="PreBlockText"/>.
    /// </summary>
    public class WebSongFetchResult
    {
        /// <summary>Original URL that was requested.</summary>
        public string SourceUrl { get; set; } = string.Empty;

        /// <summary>Hostname that produced the result (e.g. "bradwarden.com").</summary>
        public string? SourceHost { get; set; }

        /// <summary>Title extracted from the page (typically &lt;h1&gt;/&lt;h2&gt;).</summary>
        public string? Title { get; set; }

        /// <summary>Song number extracted from the page or URL (e.g. ?num=1).</summary>
        public int? SongNumber { get; set; }

        /// <summary>
        /// Concatenated text content of every relevant &lt;pre&gt; block on the page,
        /// with &lt;br&gt; replaced by '\n', HTML entities decoded, &amp;nbsp; converted
        /// to spaces, and CR/LF normalized. Ready to feed into
        /// <c>ChordLyricExtrator.ExtractFromColumnAlignedText</c>.
        /// </summary>
        public string PreBlockText { get; set; } = string.Empty;

        /// <summary>True when the URL pattern was recognized and a strategy ran.</summary>
        public bool IsSupported { get; set; }

        /// <summary>
        /// When false, the host is not on the allowlist and no fetch was performed.
        /// In future this triggers a Google-search fallback path.
        /// </summary>
        public bool RequiresWebSearchFallback { get; set; }

        /// <summary>Optional human-readable diagnostic (parse warnings, fallback reason, etc.).</summary>
        public string? Message { get; set; }
    }
}
