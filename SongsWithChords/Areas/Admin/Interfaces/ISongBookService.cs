using FRELODYAPP.Dtos;
using FRELODYLIB.ServiceHandler.ResultModels;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ISongBookService
    {
        Task<ServiceResult<List<SongBookDto>>> GetAllSongBooks();
    }
}