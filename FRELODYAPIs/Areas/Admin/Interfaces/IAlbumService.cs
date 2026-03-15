using FRELODYAPP.Dtos;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface IAlbumService
    {
        Task<ServiceResult<List<AlbumDto>>> GetAllAlbums();
        Task<ServiceResult<List<AlbumDto>>> GetAlbumsByArtistId(string artistId);
        Task<ServiceResult<List<SongDto>>> GetAllSongsByAlbumId(string albumId);
        Task<ServiceResult<AlbumDto>> CreateAlbum(AlbumDto albumDto);
        Task<ServiceResult<AlbumDto>> UpdateAlbum(string albumId, AlbumDto albumDto);
        Task<ServiceResult<AlbumDto>> GetAlbumById(string albumId);
    }
}