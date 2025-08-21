using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;

namespace FRELODYAPP.Areas.Admin.LogicData
{
    public interface IChordChartService
    {
        Task<ServiceResult<ChordChartEditDto>> CreateChordChartAsync(ChordChartCreateDto chartDto);
        Task<ServiceResult<bool>> DeleteChordChartAsync(string id);
        Task<ServiceResult<ChartWithParentChordDto>> GetChartWithParentChordByIdAsync(string id);
        Task<ServiceResult<ChordChartEditDto>> GetChordChartByIdAsync(string id);
        Task<ServiceResult<List<ChordChartEditDto>>> GetChordChartsAsync();
        Task<ServiceResult<ChordChartEditDto>> UpdateChordChartAsync(ChordChartEditDto chartDto);
    }
}