using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using Refit;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IYouTubeApi
    {
        [Get("/api/youtube/search")]
        Task<IApiResponse<List<YouTubeVideoDto>>> SearchAsync([Query] string q, [Query] int limit = 10);

        [Get("/api/youtube/videos/{videoId}")]
        Task<IApiResponse<YouTubeVideoDto>> GetVideoAsync(string videoId);

        [Post("/api/youtube/analyze")]
        Task<IApiResponse<YouTubeTranscriptionDto>> AnalyzeAsync([Body] YouTubeAnalyzeRequest request);

        [Get("/api/youtube/transcriptions/{videoId}")]
        Task<IApiResponse<YouTubeTranscriptionDto>> GetTranscriptionAsync(
            string videoId,
            [Query] string beatModel = "beat-transformer",
            [Query] string chordModel = "chord-cnn-lstm",
            [Query] string chordDict = "full");
    }
}
