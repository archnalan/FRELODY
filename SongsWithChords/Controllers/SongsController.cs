using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYAPP.Controllers
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
		[ProducesResponseType(typeof(IEnumerable<ComboBoxDto>), 200)]
        public async Task<IActionResult> GetSongs()
		{
			var songResult = await _songService.GetSongsAsync();

            if (!songResult.IsSuccess)
                return StatusCode(songResult.StatusCode, new { message = songResult.Error.Message });

            return Ok(songResult.Data);
		}

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(IEnumerable<SongDto>), 200)]
        public async Task<ActionResult<IEnumerable<SongDto>>> GetSongWithChordsById(string id)
        {
            var songResult = await _songService.GetSongById(id);

            if (!songResult.IsSuccess)
                return StatusCode(songResult.StatusCode, new { message = songResult.Error.Message });

            return Ok(songResult.Data);
        }


        [HttpGet("{id}")]
        [ProducesResponseType(typeof(IEnumerable<SongDto>), 200)]
        public async Task<ActionResult<IEnumerable<SongDto>>> GetSongDetailsById(string id)
        {
            var songResult = await _songService.GetSongDetailsById(id);

            if (!songResult.IsSuccess)
                return StatusCode(songResult.StatusCode, new { message = songResult.Error.Message });

            return Ok(songResult.Data);
        }

    }
}
