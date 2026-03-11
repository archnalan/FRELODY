using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ISongAiService
    {
        Task<ServiceResult<List<SegmentCreateDto>>> RefineExtractionAsync(string originalContent, List<SegmentCreateDto> segments);
    }
}
