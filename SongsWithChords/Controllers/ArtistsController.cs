using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYSHRD.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ArtistsController : ControllerBase
    {
        private readonly IArtistService _artistService;

        public ArtistsController(IArtistService artistService)
        {
            _artistService = artistService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ArtistDto>), 200)]
        public async Task<IActionResult> GetAllArtists()
        {
            var result = await _artistService.GetAllArtists();

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ArtistDto), 200)]
        public async Task<IActionResult> GetArtistById([FromQuery] string id)
        {
            var result = await _artistService.GetArtistById(id);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ArtistDto), 201)]
        public async Task<IActionResult> CreateArtist([FromBody] ArtistDto artistDto)
        {
            var result = await _artistService.CreateArtist(artistDto);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return CreatedAtAction(nameof(GetArtistById), new { id = result.Data.Id }, result.Data);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> DeleteArtist([FromQuery] string id)
        {
            var result = await _artistService.DeleteArtist(id);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }
    }
}