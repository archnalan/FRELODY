using FRELODYSHRD.Dtos.CreateDtos;
using Refit;

namespace FRELODYUI.Shared.RefitApis
{
    public interface ISongAiApi
    {
        [Post("/api/song-ai/refine-extraction")]
        Task<IApiResponse<List<SegmentCreateDto>>> RefineExtraction([Body] SongAiRefineRequest request);
    }
}
