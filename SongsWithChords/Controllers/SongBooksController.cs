using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SongBooksController : ControllerBase
    {
        private readonly ISongBookService _songBookService;

        public SongBooksController(ISongBookService songBookService)
        {
            _songBookService = songBookService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<SongBookDto>), 200)]
        public async Task<IActionResult> GetAllSongBooks()
        {
            var result = await _songBookService.GetAllSongBooks();

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(SongBookDto), 200)]
        public async Task<IActionResult> GetSongBookById([FromQuery]string id)
        {
            var result = await _songBookService.GetSongBookById(id);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(SongBookDto), 201)]
        public async Task<IActionResult> CreateSongBook([FromBody] SongBookDto songBookDto)
        {
            var result = await _songBookService.CreateSongBook(songBookDto);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return CreatedAtAction(nameof(GetSongBookById), new { id = result.Data.Id }, result.Data);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> DeleteSongBook([FromQuery] string id)
        {
            var result = await _songBookService.DeleteSongBook(id);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }
    }
}
