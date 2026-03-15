using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;

namespace FRELODYAPP.Areas.Admin.LogicData
{
    public interface IChordChartService
    {
        Task<ServiceResult<ChordChartEditDto>> CreateChordChartAsync(ChordChartCreateDto chartDto);
        Task<ServiceResult<ChordChartEditDto>> CreateChordChartFilesAsync(ChordChartCreateDto chartDto, IFormFile chartImage, IFormFile? chartAudio);
        Task<ServiceResult<bool>> DeleteChordChartAsync(string id);
        Task<ServiceResult<List<ChordChartEditDto>>> GetChartsByChordIdAsync(string chordId);
        Task<ServiceResult<ChartWithParentChordDto>> GetChartWithParentChordByIdAsync(string id);
        Task<ServiceResult<ChordChartEditDto>> GetChordChartByIdAsync(string id);
        Task<ServiceResult<List<ChordChartEditDto>>> GetChordChartsAsync();
        Task<ServiceResult<ChordChartEditDto>> UpdateChordChartAsync(ChordChartEditDto chartDto);
        Task<ServiceResult<ChordChartEditDto>> UpdateChordChartFilesAsync(ChordChartEditDto chartDto, IFormFile? chartImage, IFormFile? chartAudio);
    }
}