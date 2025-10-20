using FRELODYAPP.Dtos;
using FRELODYSHRD.Dtos;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IAlbumsApi
    {
        [Get("/api/albums/get-all-albums")]
        Task<IApiResponse<List<AlbumDto>>> GetAllAlbums();

        [Get("/api/albums/get-album-by-id")]
        Task<IApiResponse<AlbumDto>> GetAlbumById([Query] string albumId);

        [Get("/api/albums/get-albums-by-artist-id")]
        Task<IApiResponse<List<AlbumDto>>> GetAlbumsByArtistId([Query] string artistId);

        [Get("/api/albums/get-all-songs-by-album-id")]
        Task<IApiResponse<List<SongDto>>> GetAllSongsByAlbumId([Query] string albumId);

        [Post("/api/albums/create-album")]
        Task<IApiResponse<AlbumDto>> CreateAlbum([Body] AlbumDto albumDto);

        [Put("/api/albums/update-album")]
        Task<IApiResponse<AlbumDto>> UpdateAlbum([Body] AlbumDto albumDto);
    }
}