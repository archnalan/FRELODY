using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPIs.Areas.Admin.ViewModels;
using FRELODYAPP.Dtos;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYUI.Shared.Models.PlaylistModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PlaylistsController : ControllerBase
    {
        private readonly IPlaylistService _playlistService;

        public PlaylistsController(IPlaylistService playlistService)
        {
            _playlistService = playlistService;
        }

        [HttpGet]
        [Authorize(Roles = $"{UserRoles.Editor},{UserRoles.Contributor},{UserRoles.Admin},{UserRoles.Owner}")]
        [ProducesResponseType(typeof(List<PlaylistDto>), 200)]
        public async Task<IActionResult> GetAllPlaylists()
        {
            var result = await _playlistService.GetAllPlaylistsAsync();
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(List<PlaylistSongs>), 200)]
        public async Task<IActionResult> GetUserPlaylists([FromQuery] string userId)
        {
            var result = await _playlistService.GetUserPlaylistsAsync(userId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(PlaylistSongs), 200)]
        public async Task<IActionResult> GetPlaylistById([FromQuery] string id)
        {
            var result = await _playlistService.GetPlaylistByIdAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(PlaylistDto), 201)]
        public async Task<IActionResult> CreatePlaylist([FromBody] PlaylistDto playlist)
        {
            var result = await _playlistService.CreatePlaylistAsync(playlist);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return CreatedAtAction(nameof(GetPlaylistById), new { id = result.Data.Id }, result.Data);
        }
        
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(PlaylistDto), 201)]
        public async Task<IActionResult> AddPlaylist([FromBody] PlaylistCreateDto playlist)
        {
            var result = await _playlistService.AddPlaylistAsync(playlist);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return CreatedAtAction(nameof(GetPlaylistById), new { id = result.Data.Id }, result.Data);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(PlaylistDto), 200)]
        public async Task<IActionResult> AddSongToPlaylist([FromQuery] string playlistId, [FromQuery] string songId)
        {
            var result = await _playlistService.AddSongToPlaylistAsync(playlistId, songId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(PlaylistDto), 200)]
        public async Task<IActionResult> MakePlaylistPrivate([FromQuery] string id)
        {
            var result = await _playlistService.MakePlaylistPrivateAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpPut]
        [Authorize]
        [ProducesResponseType(typeof(PlaylistDto), 200)]
        public async Task<IActionResult> UpdatePlaylist([FromQuery] string id, [FromBody] PlaylistDto updatedPlaylist)
        {
            var result = await _playlistService.UpdatePlaylistAsync(id, updatedPlaylist);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }
        
        [HttpDelete]
        [Authorize]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> RemoveSongFromPlaylist([FromQuery] string playlistId, [FromQuery] string songId)
        {
            var result = await _playlistService.RemoveSongFromPlaylistAsync(playlistId, songId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpDelete]
        [Authorize]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> DeletePlaylist([FromQuery] string id)
        {
            var result = await _playlistService.DeletePlaylistAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(PaginationDetails<SongResult>), 200)]
        public async Task<IActionResult> GetPaginatedSongs(
        [FromQuery] int offset,
        [FromQuery] int limit,
        [FromQuery] string? songName = null,
        [FromQuery] int? songNumber = null,
        [FromQuery] string? categoryName = null,
        [FromQuery] string? songBookId = null, 
        [FromQuery] string? artistId = null,
        [FromQuery] string? albumId = null,
        [FromQuery] List<string>? curatorIds = null,
        [FromQuery] string? orderByColumn = null,
        CancellationToken cancellationToken = default)
        {
            var result = await _playlistService.GetPaginatedSongs(
                offset, limit, songName, songNumber, categoryName, songBookId, artistId, albumId, curatorIds, orderByColumn, cancellationToken);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }
        
        [HttpGet]
        [ProducesResponseType(typeof(PaginationDetails<SearchSongResult>), 200)]
        public async Task<IActionResult> EnhancedSongSearch(
            [FromQuery] int offset,
            [FromQuery] int limit,
            [FromQuery] string searchTerm,
            [FromQuery] string? orderByColumn = null,
            CancellationToken cancellationToken = default)            
        {
            var result = await _playlistService.EnhancedSongSearch(offset, limit, searchTerm, orderByColumn, cancellationToken);
            
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            
            return Ok(result.Data);
        }
    }
}