using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ISongAiService
    {
        Task<ServiceResult<List<SegmentCreateDto>>> RefineExtractionAsync(string originalContent, List<SegmentCreateDto> segments);

        /// <summary>
        /// Uses an AI vision model to refine OCR-extracted text by comparing it against the original image.
        /// Returns the refined text, or null if AI is unavailable.
        /// </summary>
        Task<string?> RefineOcrTextAsync(string ocrText, string imageBase64);
    }
}
