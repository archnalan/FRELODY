using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using Refit;

namespace FRELODYUI.Shared.RefitApis
{
    public interface ITikTokApi
    {
        [Post("/api/tik-tok/resolve")]
        Task<IApiResponse<TikTokVideoDto>> Resolve([Body] TikTokResolveRequest request);

        [Get("/api/tik-tok/get-video")]
        Task<IApiResponse<TikTokVideoDto>> GetVideo([Query] string videoId);

        [Post("/api/tik-tok/analyze")]
        Task<IApiResponse<YouTubeTranscriptionDto>> Analyze([Body] TikTokAnalyzeRequest request);

        [Get("/api/tik-tok/get-transcription")]
        Task<IApiResponse<YouTubeTranscriptionDto>> GetTranscription(
            [Query] string videoId,
            [Query] string beatModel = "beat-transformer",
            [Query] string chordModel = "chord-cnn-lstm",
            [Query] string chordDict = "full");
    }
}
