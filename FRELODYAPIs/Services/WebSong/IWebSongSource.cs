using System;
using AngleSharp.Dom;
using FRELODYSHRD.Dtos;

namespace FRELODYAPIs.Services.WebSong
{
    /// <summary>
    /// Strategy for extracting a chord/lyric pre-block + metadata from a parsed HTML document.
    /// Implementations are URL-pattern matched so generic sites and well-known hosts can each
    /// have their own metadata picker while sharing the same column-aligned algorithm.
    /// </summary>
    public interface IWebSongSource
    {
        /// <summary>Order in which sources are consulted (lowest first).</summary>
        int Priority { get; }

        /// <summary>Returns true if this source claims the given URL.</summary>
        bool CanHandle(Uri url);

        /// <summary>Extracts the pre-block text and metadata from the parsed document.</summary>
        WebSongFetchResult Extract(Uri url, IDocument document);
    }
}
