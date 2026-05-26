using FRELODYSHRD.Dtos;
using Refit;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IYouTubeApi
    {
        [Get("/api/youtube/search")]
        Task<IApiResponse<List<YouTubeVideoDto>>> SearchAsync([Query] string q, [Query] int limit = 10);

        [Get("/api/youtube/videos/{videoId}")]
        Task<IApiResponse<YouTubeVideoDto>> GetVideoAsync(string videoId);
    }
}
