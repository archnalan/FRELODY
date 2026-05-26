using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using Refit;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IYouTubeApi
    {
        [Get("/api/you-tube/search")]
        Task<IApiResponse<List<YouTubeVideoDto>>> Search([Query] string q, [Query] int limit = 10);

        [Get("/api/you-tube/get-video")]
        Task<IApiResponse<YouTubeVideoDto>> GetVideo([Query] string videoId);

        [Post("/api/you-tube/analyze")]
        Task<IApiResponse<YouTubeTranscriptionDto>> Analyze([Body] YouTubeAnalyzeRequest request);

        [Get("/api/you-tube/get-transcription")]
        Task<IApiResponse<YouTubeTranscriptionDto>> GetTranscription(
            [Query] string videoId,
            [Query] string beatModel = "beat-transformer",
            [Query] string chordModel = "chord-cnn-lstm",
            [Query] string chordDict = "full");
    }
}
