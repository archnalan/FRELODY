using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;

namespace FRELODYAPP.Controllers
{
    [Route("api/[controller]/[action]")]
	[ApiController]
	public class SongsController : ControllerBase
	{
		private readonly SongDbContext _context;
		private readonly ISongService _songService;

        public SongsController(SongDbContext context, ISongService songService)
        {
            _context = context;
            _songService = songService;
        }

        [HttpGet]
		public async Task<IActionResult> Index()
		{
			var Songs = await _context.Songs
						.Include(h => h.Verses)
						.ThenInclude(v => v.LyricLines)
						.ThenInclude(ll => ll.LyricSegments)
						.ToListAsync();

			var sortedSongs = Songs
						.OrderBy(h => h.SongNumber)
						.Select(h => new
						{
							Song = h,
							Verses = h.Verses.OrderBy(v => v.VerseNumber).Select(v => new
							{
								Verse = v,
								LyricLines = v.LyricLines.OrderBy(ll => ll.LyricLineOrder).Select(ll => new
								{
									LyricLine = ll,
									LyricSegments = ll.LyricSegments.OrderBy(ls => ls.LyricOrder).ToList()
								}).ToList()
							}).ToList()
						}).ToList();

			return Ok(sortedSongs);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetSongById( Guid id)
		{
			var Song = await _context.Songs
						.Include(h => h.Verses)
						.ThenInclude(v => v.LyricLines)
						.ThenInclude(ll => ll.LyricSegments)
						.FirstOrDefaultAsync(h => h.Id == id);

			//SongDto
			return Ok(Song);
		}

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(IEnumerable<SongDto>), 200)]
        public async Task<ActionResult<IEnumerable<SongDto>>> GetSongByWithChordsById(Guid id)
        {
            var songResult = await _songService.GetSongById(id);

            if (!songResult.IsSuccess)
                return StatusCode(songResult.StatusCode, new { message = songResult.Error.Message });

            return Ok(songResult.Data);
        }

    }
}
