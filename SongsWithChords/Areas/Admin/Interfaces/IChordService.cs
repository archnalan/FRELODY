using FRELODYLIB.ServiceHandler;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Dtos.HybridDtos;

namespace FRELODYAPP.Areas.Admin.Interfaces
{
    public interface IChordService
	{
        Task<ServiceResult<PaginationDetails<ChordDto>>> GetChordsAsync(int offset, int limit);
		Task<ServiceResult<List<ChordWithChartsDto>>> GetChordsWithChartsAsync();
		Task<ServiceResult<ChordDto>> GetChordByIdAsync(string id);
		Task<ServiceResult<ChordWithChartsDto>> GetChordWithChartsByIdAsync(string id);
		Task<ServiceResult<ChordEditDto>> CreateChordAsync(ChordCreateDto chordDto);
		Task<ServiceResult<ChordDto>> CreateSimpleChordAsync(ChordDto chordDto);
        Task<ServiceResult<ChordEditDto>> UpdateChordAsync(ChordEditDto chordDto);
        Task<ServiceResult<bool>> DeleteChordAsync(string id);
        Task<ServiceResult<PaginationDetails<ChordDto>>> SearchChordsAsync(string? keywords, int offset, int limit);
    }
}
