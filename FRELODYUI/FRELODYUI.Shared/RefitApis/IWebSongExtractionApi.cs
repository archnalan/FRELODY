using FRELODYSHRD.Dtos;
using Refit;

namespace FRELODYUI.Shared.RefitApis
{
    /// <summary>
    /// Server-side fetch of a chord/lyric web page. The server returns the
    /// monospace pre-block text + metadata; the UI runs the column-aligned
    /// chord/lyric parser locally to produce a <c>SimpleSongCreateDto</c>.
    /// </summary>
    public interface IWebSongExtractionApi
    {
        [Post("/api/lyric-extraction/from-url")]
        Task<IApiResponse<WebSongFetchResult>> FetchFromUrl([Body] WebSongFetchRequest request);
    }
}
