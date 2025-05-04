using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.ServiceHandler;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ISongService
    {
        Task<ServiceResult<List<ComboBoxDto>>> GetSongsAsync();
        Task<ServiceResult<SongDto>> CreateFullSong(FullSongCreateDto s);
        Task<ServiceResult<SongDto>> CreateSimpleSong(SimpleSongCreateDto songDto);
        Task<ServiceResult<SongDto>> GetSongById(Guid id);
        Task<ServiceResult<SongDto>> GetSongDetailsById(Guid id);
    }
}