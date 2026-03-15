using FRELODYAPP.Dtos;
using FRELODYLIB.ServiceHandler.ResultModels;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ISongBookService
    {
        Task<ServiceResult<List<SongBookDto>>> GetAllSongBooks(); 
        Task<ServiceResult<SongBookDto>> GetSongBookById(string id);
        Task<ServiceResult<SongBookDto>> CreateSongBook(SongBookDto songBookDto);
        Task<ServiceResult<bool>> DeleteSongBook(string id);
    }
}