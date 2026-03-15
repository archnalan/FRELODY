using FRELODYSHRD.Dtos.CreateDtos;
using Refit;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IOcrApi
    {
        [Post("/api/song-ai/ocr-extract")]
        Task<IApiResponse<OcrExtractResult>> ExtractTextFromImage([Body] OcrExtractRequest request);
    }
}
