using FRELODYAPP.Dtos;
using FRELODYAPP.ServiceHandler;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYAPP.Areas.Admin.Interfaces
{
    public interface ISongPartService
	{
		Task<ServiceResult<List<SongPartDto>>> GetAllVersesAsync();

		Task<ServiceResult<SongPartDto>> GetVerseByIdAsync(string id);

		Task<ServiceResult<SongPartDto>> CreateVerseAsync(VerseCreateDto verseDto);

		Task<ServiceResult<SongPartDto>> EditVerseAsync(string id, SongPartDto verseEdit);

		Task<ServiceResult<bool>> DeleteVerseAsync(string id);
	}
}
