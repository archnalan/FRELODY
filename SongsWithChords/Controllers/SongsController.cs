using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYLIB.ServiceHandler;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.SubDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Areas.Admin.ApiControllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SongsController : ControllerBase
    {
        private readonly ISongService _songService;

        public SongsController(ISongService songService)
        {
            _songService = songService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SongDto>), 200)]
        public async Task<ActionResult<IEnumerable<SongDto>>> GetSongById([FromQuery] string Id)
        {
            var songResult = await _songService.GetSongById(Id);

            if (!songResult.IsSuccess)
                return StatusCode(songResult.StatusCode, new { message = songResult.Error.Message });

            return Ok(songResult.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(SongDto), 200)]
        public async Task<ActionResult<SongDto>> GetSongWithChordsById([FromQuery]string Id)
        {
            var songResult = await _songService.GetSongDetailsById(Id);
            if (!songResult.IsSuccess)
                return StatusCode(songResult.StatusCode, new { message = songResult.Error.Message });
            return Ok(songResult.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(SongDto), 200)]
        public async Task<ActionResult> CreateSong([FromBody] SimpleSongCreateDto song)
        {
            var songResult = await _songService.CreateSong(song);

            if (!songResult.IsSuccess)
                return StatusCode(songResult.StatusCode, new { message = songResult.Error.Message });

            return Ok(songResult.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(PaginationDetails<ComboBoxDto>), 200)]
        public async Task<IActionResult> GetSongs([FromQuery]int offset, [FromQuery]int limit)
        {
            var songResult = await _songService.GetSongsAsync(offset,limit);

            if (!songResult.IsSuccess)
                return StatusCode(songResult.StatusCode, new { message = songResult.Error.Message });

            return Ok(songResult.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(PaginationDetails<ComboBoxDto>), 200)]
        public async Task<IActionResult> SearchSongs([FromQuery]string? keywords, [FromQuery] int offset, [FromQuery] int limit)
        {
            var songResult = await _songService.SearchSongsAsync(keywords, offset, limit);
            
            if (!songResult.IsSuccess)
                return StatusCode(songResult.StatusCode, new { message = songResult.Error.Message });
            
            return Ok(songResult.Data);
        }

        [HttpPut]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> MarkSongFavoriteStatus([FromQuery]string songId, [FromQuery]bool favorite)
        {
            var result = await _songService.MarkSongFavoriteStatus(songId, favorite);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpPut]
        [ProducesResponseType(typeof(SongDto), 200)]
        public async Task<IActionResult> UpdateSong(string id, [FromBody] SimpleSongCreateDto songDto)
        {
            var songResult = await _songService.UpdateSong(id, songDto);
            if (!songResult.IsSuccess)
                return StatusCode(songResult.StatusCode, new { message = songResult.Error.Message });
            return Ok(songResult.Data);
        }
        
        [HttpGet]
        [ProducesResponseType(typeof(CanRateDto), 200)]
        public async Task<IActionResult> CanUserRateSong([FromQuery] string songId)
        {
            var result = await _songService.CanUserRateSong(songId);
            
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });
            
            return Ok(result.Data);
        }

        [HttpPut]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> RateSong([FromQuery] string songId, [FromQuery] decimal rating)
        {
            var result = await _songService.SetSongRating(songId, rating);
            
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });
            
            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ComboBoxDto>), 200)]
        public async Task<IActionResult> GetFavoriteSongs([FromQuery]string? userId = null)
        {
            var result = await _songService.GetFavoriteSongs(userId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> DeleteSong([FromQuery] string songId)
        {
            var result = await _songService.DeleteSong(songId);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

    }
}
