using FRELODYAPIs.Areas.Admin.ViewModels;
using FRELODYAPP.Dtos;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYUI.Shared.Models.PlaylistModels;
using System.ComponentModel.DataAnnotations;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface IPlaylistService
    {
        Task<ServiceResult<PlaylistDto>> CreatePlaylistAsync(PlaylistDto Playlist);
        Task<ServiceResult<bool>> DeletePlaylistAsync(string id);
        Task<ServiceResult<List<PlaylistDto>>> GetAllPlaylistsAsync();
        Task<ServiceResult<PaginationDetails<SongResult>>> GetPaginatedSongs(int offset, int limit, string? songName = null, int? songNumber = null, string? categoryName = null, string? songBookId = null, List<string>? curatorIds = null, string? orderByColumn = null, CancellationToken cancellationToken = default);
        Task<ServiceResult<PlaylistSongs>> GetPlaylistByIdAsync(string id);
        Task<ServiceResult<PaginationDetails<SearchSongResult>>> EnhancedSongSearch(int offset, int limit, string searchTerm, string? orderByColumn = null, CancellationToken cancellationToken = default);
        Task<ServiceResult<PlaylistDto>> UpdatePlaylistAsync(string id, PlaylistDto updatedPlaylist);
        Task<ServiceResult<List<PlaylistSongs>>> GetUserPlaylistsAsync(string userId);
        Task<ServiceResult<PlaylistDto>> MakePlaylistPrivateAsync(string id);
        Task<ServiceResult<PlaylistDto>> AddPlaylistAsync([Required] PlaylistCreateDto playlistCreateDto);
        Task<ServiceResult<bool>> RemoveSongFromPlaylistAsync(string playlistId, string songId);
        Task<ServiceResult<PlaylistDto>> AddSongToPlaylistAsync(string playlistId, string songId);
    }
}