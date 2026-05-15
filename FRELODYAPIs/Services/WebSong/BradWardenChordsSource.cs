using System;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using FRELODYSHRD.Dtos;
using Microsoft.AspNetCore.WebUtilities;

namespace FRELODYAPIs.Services.WebSong
{
    /// <summary>
    /// Host-specific source for https://bradwarden.com/music/hymnchords/?num=N.
    /// Reuses <see cref="MonospacePreBlockSource"/> for the &lt;pre&gt; extraction
    /// and adds title + song-number parsing from the page heading and query string.
    /// </summary>
    public sealed class BradWardenChordsSource : IWebSongSource
    {
        // Heading like: "1. Praise to the Lord" or "241. Jesus, the Very Thought of Thee"
        private static readonly Regex HeadingRegex = new(
            @"^\s*(?<num>\d+)\s*\.\s*(?<title>.+?)\s*$",
            RegexOptions.Compiled);

        public int Priority => 10;

        public bool CanHandle(Uri url) =>
            url.Host.EndsWith("bradwarden.com", StringComparison.OrdinalIgnoreCase) &&
            url.AbsolutePath.StartsWith("/music/hymnchords", StringComparison.OrdinalIgnoreCase);

        public WebSongFetchResult Extract(Uri url, IDocument document)
        {
            var preText = MonospacePreBlockSource.ExtractPreBlocks(document);

            string? title = null;
            int? songNumber = null;

            // Title + number from the first <h2>.
            var heading = document.QuerySelector("h2")?.TextContent?.Trim();
            if (!string.IsNullOrEmpty(heading))
            {
                var match = HeadingRegex.Match(heading);
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

            // Fallback: ?num=N from the URL.
            if (songNumber is null)
            {
                var query = QueryHelpers.ParseQuery(url.Query);
                if (query.TryGetValue("num", out var raw) && int.TryParse(raw, out var n))
                    songNumber = n;
            }

            return new WebSongFetchResult
            {
                SourceUrl = url.ToString(),
                SourceHost = url.Host,
                Title = title,
                SongNumber = songNumber,
                PreBlockText = preText,
                IsSupported = !string.IsNullOrWhiteSpace(preText),
                Message = string.IsNullOrWhiteSpace(preText)
                    ? "No <pre> chord block was found on this page."
                    : null
            };
        }
    }
}
