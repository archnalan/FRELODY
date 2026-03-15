using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface IOcrService
    {
        Task<ServiceResult<OcrExtractResult>> ExtractTextFromImageAsync(byte[] imageData, string fileName);
    }
}
