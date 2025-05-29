using AutoMapper;
using FRELODYAPP.Areas.Admin.Interfaces;
using FRELODYAPP.Areas.Admin.LogicData;
using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.CompositeDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;
using System.Transactions;
using FRELODYAPP.Data.Infrastructure;
using FRELODYSHRD.Dtos.EditDtos;

namespace FRELODYAPP.Areas.Admin.ApiControllers
{
    [Route("api/[controller]")]
	[ApiController]
	public class VersesController : ControllerBase
	{
		private readonly IVerseService _verseService;
		private readonly ILyricLineService _lineService;
		private readonly ILyricSegment _segmentService;
		private readonly IChordService _chordService;
		private readonly IMapper _mapper;
		private readonly SongDbContext _context;
        public VersesController(IVerseService verseService, ILyricLineService lineService, IMapper mapper,
						ILyricSegment segmentService, IChordService chordService, SongDbContext context)
        {
			_verseService = verseService;
			_lineService = lineService;
			_segmentService = segmentService;
			_chordService = chordService;
			_context = context;
			_mapper = mapper;
        }

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var verseResult = await _verseService.GetAllVersesAsync();

			if (verseResult.IsSuccess == false || verseResult.Data == null) 
			{ 
				return NotFound(); 
			}

			return Ok(verseResult.Data);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetVerseById(string id)
		{
			var verseResult = await _verseService.GetVerseByIdAsync(id);

			if(verseResult.IsSuccess == false) 
				return StatusCode(verseResult.StatusCode, new {message = verseResult.Error.Message});

			return Ok(verseResult.Data);
		}

		[HttpGet("by_ids")]
		public async Task<IActionResult> GetVersesByIds(List<string> ids)
		{
			if(ids.Count == 0 || !ids.Any())
			{
				return BadRequest("Verse Ids are required");
			}

			var foundVerses = new List<VerseDto>();
			var responseErrors = new List<(string id, string errorMessage)>();
			
			foreach(var id in ids)
			{
				var verseResult = await _verseService.GetVerseByIdAsync(id);

				if (verseResult.IsSuccess == false)
					responseErrors.Add((id, verseResult.Error.Message));

				foundVerses.Add(verseResult.Data);
			}

			if(responseErrors.Count == ids.Count)
			{
				return StatusCode(StatusCodes.Status404NotFound, new {Errors = responseErrors});
			}

			if (responseErrors.Any())
			{
				return Ok(new
				{
					Found = foundVerses,
					NotFound = responseErrors
				});
			}

			return Ok(foundVerses);
		}

		[HttpPost("create")]
		public async Task<IActionResult> CreateVerse([FromBody]CreateFullVerseDto createFullVerse)
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			using(var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{

					var verseResult = await _verseService.CreateVerseAsync(createFullVerse.verseCreate);

					if (verseResult.IsSuccess == false || verseResult.Data == null)
						return StatusCode(verseResult.StatusCode, new { message = verseResult.Error.Message });
					
					var createdVerse = verseResult.Data;

					var verseId = createdVerse.Id;

					var lines = new List<LyricLineDto>();
					var segments = new List<LyricSegmentDto>();
					var chords = new List<ChordEditDto>();
					var errorLines = new List<(long orderNumber, string errorMessage)>();
					var errorSegments = new List<(long orderNumber, string errorMessage)>();
					var errorChords = new List<(string ChordName, string errorMessage)>();

					foreach (var line in createFullVerse.linesCreate)
					{
						line.VerseId = verseId;
						var lineResult = await _lineService.CreateVerseLineAsync(line);

						if (lineResult.IsSuccess == false)
						{
							errorLines.Add((line.LyricLineOrder, lineResult.Error.Message));
							continue;
						}

						var createdLine = lineResult.Data;
						lines.Add(createdLine);

						var lineId = createdLine.Id;

						foreach (var segment in createFullVerse.segmentsCreate     //lineOrderNumber acts as temporary 
							.Where(seg => seg.UISegNo == line.LyricLineOrder)) //segment ID from the front end
						{
							segment.LyricLineId = lineId;
							var segmentResult = await _segmentService.CreateSegmentAsync(segment);

							if (segmentResult.IsSuccess == false)
							{
								errorSegments.Add((segment.LyricOrder, segmentResult.Error.Message));
								continue;
							}

							var createdSegment = segmentResult.Data;
							segments.Add(createdSegment);

							var SegmentId = createdSegment.Id;

							var segChord = createFullVerse.chordsCreate;
							if (segChord != null && segment.ChordId != null)
							{
								foreach (var chord in createFullVerse.chordsCreate     //lineNumber and segment orderNumber are crucial 
								.Where(ch => ch.UIChordNo == line.LyricLineOrder && ch.SegmentOrderNo == segment.LyricOrder))
								{
									var chordInDb = await _context.Chords
														.FirstOrDefaultAsync(ch => ch.ChordName == chord.ChordName);

									if (chordInDb != null)
									{
										segment.ChordId = chordInDb.Id;
										var chordAssigned = _mapper.Map<ChordEditDto>(chordInDb);

										chords.Add(chordAssigned);
									}
									else
									{
										var chordCreate = await _chordService.CreateChordAsync(chord);

										if (chordCreate.IsSuccess == false)
											errorChords.Add((chord.ChordName, chordCreate.Error.Message));

										var createdChord = chordCreate.Data;
										var createdChordId = createdChord.Id;
										chords.Add(createdChord);

										segment.ChordId = createdChordId;
									}

								}
							}
						}

                    }

					if (errorLines.Any() || errorSegments.Any() || errorChords.Any())
					{
						await transaction.RollbackAsync();
						return BadRequest(new { errorLines, errorSegments, errorChords });
					}

					await transaction.CommitAsync();

					return Ok(new { createdVerse, lines, segments, chords});
				}
				catch(Exception ex)
				{
					await transaction.RollbackAsync();
					return StatusCode(500, new { message = $"Error Creating Verse: {ex.Message}" });
				}
			}

		}
	}	
}
