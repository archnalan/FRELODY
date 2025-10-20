using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using FRELODYSHRD.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AlbumsController : ControllerBase
    {
        private readonly IAlbumService _albumService;

        public AlbumsController(IAlbumService albumService)
        {
            _albumService = albumService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<AlbumDto>), 200)]
        public async Task<IActionResult> GetAllAlbums()
        {
            var result = await _albumService.GetAllAlbums();
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(AlbumDto), 200)]
        public async Task<IActionResult> GetAlbumById([FromQuery] string albumId)
        {
            var result = await _albumService.GetAlbumById(albumId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<AlbumDto>), 200)]
        public async Task<IActionResult> GetAlbumsByArtistId([FromQuery] string artistId)
        {
            var result = await _albumService.GetAlbumsByArtistId(artistId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<SongDto>), 200)]
        public async Task<IActionResult> GetAllSongsByAlbumId([FromQuery] string albumId)
        {
            var result = await _albumService.GetAllSongsByAlbumId(albumId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(AlbumDto), 200)]
        public async Task<IActionResult> CreateAlbum([FromBody] AlbumDto albumDto)
        {
            var result = await _albumService.CreateAlbum(albumDto);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }

        [HttpPut]
        [ProducesResponseType(typeof(AlbumDto), 200)]
        public async Task<IActionResult> UpdateAlbum([FromBody] AlbumDto albumDto)
        {
            var result = await _albumService.UpdateAlbum(albumDto.Id, albumDto);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error);
            return Ok(result.Data);
        }
    }
}