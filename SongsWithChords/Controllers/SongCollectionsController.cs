using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPIs.Areas.Admin.ViewModels;
using FRELODYAPP.Dtos;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.CreateDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SongCollectionsController : ControllerBase
    {
        private readonly ISongCollectionService _songCollectionService;

        public SongCollectionsController(ISongCollectionService songCollectionService)
        {
            _songCollectionService = songCollectionService;
        }

        [HttpGet]
        [Authorize(Roles = $"{UserRoles.Editor},{UserRoles.Contributor},{UserRoles.Admin},{UserRoles.Owner}")]
        [ProducesResponseType(typeof(List<SongCollectionDto>), 200)]
        public async Task<IActionResult> GetAllSongCollections()
        {
            var result = await _songCollectionService.GetAllSongCollectionsAsync();
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(List<SongCollectionDto>), 200)]
        public async Task<IActionResult> GetUserSongCollections([FromQuery] string userId)
        {
            var result = await _songCollectionService.GetUserSongCollectionsAsync(userId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(SongCollectionDto), 200)]
        public async Task<IActionResult> GetSongCollectionById([FromQuery] string id)
        {
            var result = await _songCollectionService.GetSongCollectionByIdAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(SongCollectionDto), 201)]
        public async Task<IActionResult> CreateSongCollection([FromBody] SongCollectionDto collection)
        {
            var result = await _songCollectionService.CreateSongCollectionAsync(collection);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return CreatedAtAction(nameof(GetSongCollectionById), new { id = result.Data.Id }, result.Data);
        }
        
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(SongCollectionDto), 201)]
        public async Task<IActionResult> AddCollection([FromBody] SongCollectionCreateDto collection)
        {
            var result = await _songCollectionService.AddCollectionAsync(collection);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return CreatedAtAction(nameof(GetSongCollectionById), new { id = result.Data.Id }, result.Data);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(SongCollectionDto), 200)]
        public async Task<IActionResult> MakeCollectionPrivate([FromQuery] string id)
        {
            var result = await _songCollectionService.MakeCollectionPrivateAsync(id);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpPut]
        [Authorize]
        [ProducesResponseType(typeof(SongCollectionDto), 200)]
        public async Task<IActionResult> UpdateSongCollection([FromQuery] string id, [FromBody] SongCollectionDto updatedCollection)
        {
            var result = await _songCollectionService.UpdateSongCollectionAsync(id, updatedCollection);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpDelete]
        [Authorize]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> DeleteSongCollection([FromQuery] string id)
        {
            var result = await _songCollectionService.DeleteSongCollectionAsync(id);
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
        [FromQuery] List<string>? curatorIds = null,
        [FromQuery] string? orderByColumn = null,
        CancellationToken cancellationToken = default)
        {
            var result = await _songCollectionService.GetPaginatedSongs(
                offset, limit, songName, songNumber, categoryName, songBookId, curatorIds, orderByColumn, cancellationToken);

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
            var result = await _songCollectionService.EnhancedSongSearch(offset, limit, searchTerm, orderByColumn, cancellationToken);
            
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            
            return Ok(result.Data);
        }
    }
}