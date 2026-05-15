namespace FRELODYSHRD.Dtos
{
    /// <summary>
    /// Request payload for server-side fetch of a chord/lyric web page.
    /// </summary>
    public class WebSongFetchRequest
    {
        public string Url { get; set; } = string.Empty;
    }
}
