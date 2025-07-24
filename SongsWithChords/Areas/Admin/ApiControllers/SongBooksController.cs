using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Areas.Admin.ApiControllers
{
    [Route("api/[controller]")]
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
    }
}
