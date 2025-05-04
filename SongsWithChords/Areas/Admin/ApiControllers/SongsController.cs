using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYSHRD.Dtos.CreateDtos;
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
        public async Task<ActionResult<IEnumerable<SongDto>>> GetSongById(Guid Id)
        {
            var songResult = await _songService.GetSongById(Id);

            if (!songResult.IsSuccess)
                return StatusCode(songResult.StatusCode, new { message = songResult.Error.Message });

            return Ok(songResult.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(SongDto), 200)]
        public async Task<ActionResult<SongDto>> CreateSimpleSong([FromBody] SimpleSongCreateDto song)
        {
            var songResult = await _songService.CreateSimpleSong(song);

            if (!songResult.IsSuccess)
                return StatusCode(songResult.StatusCode, new { message = songResult.Error.Message });

            return Ok(songResult.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(SongDto), 200)]
        public async Task<ActionResult<SongDto>> CreateFullSong([FromBody] FullSongCreateDto song)
        {
            var songResult = await _songService.CreateFullSong(song);

            if (!songResult.IsSuccess)
                return StatusCode(songResult.StatusCode, new { message = songResult.Error.Message });

            return Ok(songResult.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ComboBoxDto>), 200)]
        public async Task<IActionResult> GetAllSongs()
        {
            var songResult = await _songService.GetSongsAsync();

            if (!songResult.IsSuccess)
                return StatusCode(songResult.StatusCode, new { message = songResult.Error.Message });

            return Ok(songResult.Data);
        }

    }
}
