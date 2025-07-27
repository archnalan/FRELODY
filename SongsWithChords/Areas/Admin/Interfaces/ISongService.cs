using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.ServiceHandler;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ISongService
    {
        Task<ServiceResult<List<ComboBoxDto>>> GetSongsAsync();
        Task<ServiceResult<SongDto>> CreateSong(SimpleSongCreateDto songDto);
        Task<ServiceResult<SongDto>> GetSongById(string id);
        Task<ServiceResult<SongDto>> GetSongDetailsById(string id);
        Task<ServiceResult<bool>> MarkSongFavoriteStatus(string songId, bool favorite);
    }
}