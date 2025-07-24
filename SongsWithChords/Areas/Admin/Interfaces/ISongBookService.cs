using FRELODYAPP.Dtos;
using FRELODYAPP.ServiceHandler;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ISongBookService
    {
        Task<ServiceResult<List<SongBookDto>>> GetAllSongBooks();
    }
}