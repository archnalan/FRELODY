using FRELODYAPP.Dtos;
using FRELODYAPP.ServiceHandler;

namespace FRELODYAPP.Areas.Admin.Interfaces
{
	public interface IChordService
	{
		Task<ServiceResult<List<ChordEditDto>>> GetAllChordsAsync();
		Task<ServiceResult<List<ChordWithChartsDto>>> GetChordsWithChartsAsync();
		Task<ServiceResult<ChordEditDto>> GetChordByIdAsync(long id);
		Task<ServiceResult<ChordWithChartsDto>> GetChordWithChartsByIdAsync(long id);
		Task<ServiceResult<ChordEditDto>> CreateChordAsync(ChordCreateDto chordDto);
		Task<ServiceResult<ChordEditDto>> UpdateChordAsync(ChordEditDto chordDto);
        Task<ServiceResult<bool>> DeleteChordAsync(long id);
    }
}
