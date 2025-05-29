using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.CompositeDtos;
using FRELODYAPP.ServiceHandler;

namespace FRELODYAPP.Areas.Admin.Interfaces
{
	public interface ILyricLineService
	{
		Task<ServiceResult<List<LyricLineDto>>> GetAllLyricLinesAsync();

		Task<ServiceResult<LyricLineDto>> GetLyricLineByIdAsync(string id);

		Task<ServiceResult<LyricLineDto>> CreateVerseLineAsync(LineVerseCreateDto verselineDto);

		Task<ServiceResult<LyricLineDto>> EditVerseLineAsync(string id, LyricLineDto verseLineDto);

		Task<ServiceResult<bool>> DeleteLyricLineAsync(string id);
	}
}
