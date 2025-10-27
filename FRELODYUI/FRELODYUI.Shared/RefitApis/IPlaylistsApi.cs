using FRELODYAPIs.Areas.Admin.ViewModels;
using FRELODYAPP.Dtos;
using FRELODYLIB.ServiceHandler;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYUI.Shared.Models.PlaylistModels;
using Refit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IPlaylistsApi
    {
        [Get("/api/playlists/get-all-playlists")]
        Task<IApiResponse<List<PlaylistDto>>> GetAllPlaylists();

        [Get("/api/playlists/get-user-playlists")]
        Task<IApiResponse<List<PlaylistSongs>>> GetUserPlaylists([Query] string userId);

        [Get("/api/playlists/get-playlists-by-id")]
        Task<IApiResponse<PlaylistSongs>> GetPlaylistById([Query] string id);

        [Post("/api/playlists/create-playlist")]
        Task<IApiResponse<PlaylistDto>> CreatePlaylist([Body] PlaylistDto playlist);
        
        [Post("/api/playlists/add-playlist")]
        Task<IApiResponse<PlaylistDto>> AddPlaylist([Body] PlaylistCreateDto playlist);

        [Post("/api/playlists/add-song-to-playlist")]
        Task<IApiResponse<PlaylistDto>> AddSongToPlaylist([Query] string playlistId, [Query] string songId);

        [Post("/api/playlists/make-playlist-private")]
        Task<IApiResponse<PlaylistDto>> MakePlaylistPrivate([Query] string id);

        [Put("/api/playlists/update-playlist")]
        Task<IApiResponse<PlaylistDto>> UpdatePlaylist([Query] string id, [Body] PlaylistDto updatedPlaylist);
        
        [Delete("/api/playlists/remove-song-from-playlist")]
        Task<IApiResponse<bool>> RemoveSongFromPlaylist([Query] string playlistId, [Query] string songId);

        [Delete("/api/playlists/delete-playlist")]
        Task<IApiResponse<bool>> DeletePlaylist([Query] string id);

        [Get("/api/playlists/get-paginated-songs")]
        Task<IApiResponse<PaginationDetails<SongResult>>> GetPaginatedSongs(
        [Query] int offset,
        [Query] int limit,
        [Query] string? songName = null,
        [Query] int? songNumber = null,
        [Query] string? categoryName = null,
        [Query] string? songBookId = null, 
        [Query] string? artistId = null,
        [Query] string? albumId = null,
        [Query] List<string>? curatorIds = null,
        [Query] string? orderByColumn = null,
        CancellationToken cancellationToken = default);
      
        [Get("/api/playlists/enhanced-song-search")]
        Task<IApiResponse<PaginationDetails<SearchSongResult>>> EnhancedSongSearch(
            [Query] int offset,
            [Query] int limit,
            [Query] string searchTerm,
            [Query] string? orderByColumn = null,
            CancellationToken cancellationToken = default);            
    }
}