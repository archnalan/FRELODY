using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYLIB.ServiceHandler;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.SubDtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ISongService
    {
        Task<ServiceResult<PaginationDetails<ComboBoxDto>>> GetSongsAsync(int offset, int limit);
        Task<ServiceResult<PaginationDetails<ComboBoxDto>>> SearchSongsAsync(string? keywords, int offset, int limit);
        Task<ServiceResult<SongDto>> CreateSong(SimpleSongCreateDto songDto);
        Task<ServiceResult<SongDto>> GetSongById(string id);
        Task<ServiceResult<SongDto>> GetSongDetailsById(string id);
        Task<ServiceResult<bool>> MarkSongFavoriteStatus(string songId, bool favorite);
        Task<ServiceResult<SongDto>> UpdateSong(string id, SimpleSongCreateDto songDto);
        Task<ServiceResult<bool>> SetSongRating(string songId, decimal rating);
        Task<ServiceResult<CanRateDto>> CanUserRateSong(string songId);
        Task<ServiceResult<bool>> DeleteSong(string songId);
        Task<ServiceResult<PaginationDetails<ComboBoxDto>>> GetFavoriteSongs(string? userId = null, int? offset = 0, int? limit = 10);
        Task<ServiceResult<bool>> IsSongFavorited(string songId,string? userId = null);
    }
}