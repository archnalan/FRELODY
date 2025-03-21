using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FRELODYAPP.Data.Infrastructure;

namespace FRELODYAPP.Controllers
{
    [Route("api/[controller]")]
	[ApiController]
	public class SongsController : ControllerBase
	{
		private readonly SongDbContext _context;

        public SongsController(SongDbContext context)
        {
            _context = context;
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
		public async Task<IActionResult> GetSongById(Guid id)
		{
			var Song = await _context.Songs
						.Include(h => h.Verses)
						.ThenInclude(v => v.LyricLines)
						.ThenInclude(ll => ll.LyricSegments)
						.FirstOrDefaultAsync(h => h.Id == id);

			//SongDto
			return Ok(Song);
		}

		//[HttpGet("chords")]
		//public async Task<IActionResult> GetFRELODYAPP()
		//{
		//	var Songs = await _context.Songs
		//				.Include(h => h.Verses)
		//				.ThenInclude(v => v.LyricLines)
		//				.ThenInclude(ll => ll.LyricSegments)
		//				.ThenInclude(ls => ls.Chord)
		//				.ThenInclude(c => c.ChordCharts)
		//				.ToListAsync();

		//	var SongWithChords = Songs
		//				.OrderBy(h => h.SongNumber)
		//				.Select(h => new SongChordsUIDto
		//				{
		//					Title = h.Title,
  //                          SongNumber = h.SongNumber,
		//					Verses = h.Verses.OrderBy(v => v.VerseNumber).Select(v => new VerseUIDto
		//					{
		//						VerseNumber = v.VerseNumber,
		//						LyricLines = v.LyricLines.OrderBy(ll => ll.LyricLineOrder).Select(ll => new LyricLineUIDto
		//						{
		//							LyricLineOrder = ll.LyricLineOrder,
		//							LyricSegments = ll.LyricSegments.OrderBy(ls => ls.LyricOrder).Select(ls => new LyricSegmentUIDto
		//							{
		//								LyricOrder = ls.LyricOrder,
		//								Chord = ls.Chord == null ? null : new ChordUIDto
		//								{
		//									Id = ls.Chord.Id,
		//									ChordCharts = ls.Chord.ChordCharts.OrderBy(cc => cc.FretPosition).Select(cc => new ChordChartUIDto
		//									{
		//										Id = cc.Id,
		//										FretPosition = cc.FretPosition
		//									}).ToList()
		//								}
		//							}).ToList()
		//						}).ToList()
		//					}).ToList()
		//				}).ToList();

		//	return Ok(SongWithChords);
		//}


	}
}
