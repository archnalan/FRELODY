using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.CompositeDtos;
using FRELODYAPP.ServiceHandler;

namespace FRELODYAPP.Areas.Admin.Interfaces
{
	public interface ILyricLineService
	{
		Task<ServiceResult<List<LyricLineDto>>> GetAllLyricLinesAsync();

		Task<ServiceResult<LyricLineDto>> GetLyricLineByIdAsync(Guid id);

		Task<ServiceResult<LyricLineDto>> CreateVerseLineAsync(LineVerseCreateDto verselineDto);

		Task<ServiceResult<LyricLineDto>> EditVerseLineAsync(Guid id, LyricLineDto verseLineDto);

		Task<ServiceResult<bool>> DeleteLyricLineAsync(Guid id);
	}
}
