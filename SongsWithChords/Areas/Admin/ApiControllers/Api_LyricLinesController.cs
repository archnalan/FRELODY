using AutoMapper;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using FRELODYAPP.Dtos;
using FRELODYAPP.Models;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FRELODYAPP.Data.Infrastructure;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYAPP.Areas.Admin.ApiControllers
{
    [Route("admin/[controller]")]
	[ApiController]
	[Area("Admin")]
	public class Api_LyricLinesController : ControllerBase
	{
		private readonly SongDbContext _context;
		private readonly IMapper _mapper;

        public Api_LyricLinesController(SongDbContext context, IMapper mapper)
        {
            _context = context;
			_mapper = mapper;
        }

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var lyricLines = await _context.LyricLines
								.OrderBy(ll=>ll.LyricLineOrder)
								.ToListAsync();

			var lyricLineDto = _mapper.Map<List<LyricLineDto>>(lyricLines);

			return Ok(lyricLineDto);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetLyricLineById(string id)
		{
			var lyricLine = await _context.LyricLines.FindAsync(id);

			if(lyricLine == null) return NotFound($"Lyric Line with ID:{id} does not exist.");

			var lyricLineDto = _mapper.Map<LyricLineDto>(lyricLine);

			return Ok(lyricLineDto);
		}

		[HttpGet("verse/{id}")]
		public async Task<IActionResult> GetLyricLinesInVerse(string id)
		{
			var lyricLines = await _context.LyricLines
								.Where(ll => ll.VerseId == id)
								.Include(ll => ll.LyricSegments)
								.ToListAsync();

			if (lyricLines == null) return NotFound($"No Lyric Lines exist for verse of ID:{id}");

			var lyricLineDtos = _mapper.Map<List<LyricLineDto>>(lyricLines);

			return Ok(lyricLineDtos);
		}

		[HttpGet("bridge/{id}")]
		public async Task<IActionResult> GetLyricLinesInBridge(string id)
		{
			var lyricLines = await _context.LyricLines
								.Where(ll => ll.BridgeId == id)
								.Include(ll => ll.LyricSegments)
								.ToListAsync();

			if (lyricLines.Any() == false) return NotFound($"No lyric lines exist for verse of ID:{id}");

			var lyricLineDtos = _mapper.Map<List<LyricLineDto>>(lyricLines);

			return Ok(lyricLineDtos);
		}
		
		[HttpGet("chorus/{id}")]
		public async Task<IActionResult> GetLyricLinesInChorus(string id)
		{
			var lyricLines = await _context.LyricLines
								.Where(ll => ll.ChorusId == id)
								.Include(ll=>ll.LyricSegments)
								.ToListAsync();

			if (lyricLines.Any() == false) return NotFound($"No lyric lines exist for verse of ID:{id}");

			var lyricLineDtos = _mapper.Map<List<LyricLineDto>>(lyricLines);

			return Ok(lyricLineDtos);
		}

		[HttpPost("create_verseline")]
		public async Task<IActionResult> CreateVerseLine(LyricLineCreateDto verselineDto)
		{
			if (verselineDto == null) return BadRequest("Verse data is required.");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			if (verselineDto.VerseId != null)
			{
				var VerseExists = await _context.Verses
									.AnyAsync(ll => ll.Id == verselineDto.VerseId);

				if (VerseExists == false)
					return BadRequest($"Parent Verse Id:{verselineDto.VerseId} does not exist");
			}
			else
			{
				return BadRequest("Verse Id is required.");
			}

			//No LyricOrderNumber is duplicated within the same verse
			var verseLineExists = await _context.LyricLines
									.Where(vl => vl.VerseId == verselineDto.VerseId)// Filter by the same verse
									.AnyAsync(vl=> vl.LyricLineOrder == verselineDto.LyricLineOrder);

			if (verseLineExists)
				return Conflict($"Lyric line Order value:{verselineDto.LyricLineOrder} already taken.");

			var verse = _mapper.Map<LyricLine>(verselineDto);

			_context.LyricLines.Add(verse);
			await _context.SaveChangesAsync();

			var verseDto = _mapper.Map<LyricLineDto>(verse);

			return Ok(verseDto);
		}

		[HttpPost("create_verselines")]
		public async Task<IActionResult> CreateVerseLines(List<LyricLineCreateDto> verselineDtos)
		{
			if (verselineDtos == null || verselineDtos.Count == 0) 
				return BadRequest("Verse data is required.");

			var verseLinesToAdd = new List<LyricLine>();
			var createdVerseLineDtos = new List<LyricLineDto>();

			var errors = new List<string>();
			var noRepeatLineOrder = new System.Collections.Generic
										.HashSet<(long? LyricOrder, string? VerseId)>();

			foreach(var verselineDto in verselineDtos)
			{
				if (!TryValidateModel(verselineDto))
				{
					errors.Add($"Invalid data for line number:{verselineDto.LyricLineOrder}");
					continue;
				}

				if (verselineDto.VerseId != null)
				{
					var VerseExists = await _context.Verses
										.AnyAsync(ll => ll.Id == verselineDto.VerseId);

					if (VerseExists == false)
					{
						errors.Add($"Parent Verse Id:{verselineDto.VerseId} does not exist");
						continue;
					}						
				}
				else
				{
					errors.Add("Verse Id is required.");
					continue;
				}

				//No LyricOrderNumber is duplicated within the same verse
				var verseLineExists = await _context.LyricLines
										.Where(vl => vl.VerseId == verselineDto.VerseId)// Filter by the same verse
										.AnyAsync(vl => vl.LyricLineOrder == verselineDto.LyricLineOrder);

				if (verseLineExists)
				{
					errors.Add($"Lyric line Order value:{verselineDto.LyricLineOrder} already taken.");
					continue;
				}

				if(noRepeatLineOrder.Contains((verselineDto.LyricLineOrder, verselineDto.VerseId)))
				{
					errors.Add($"Invalid! Duplicate line no.{verselineDto.LyricLineOrder} detected for same verse");
				}

				var verseToAdd = _mapper.Map<LyricLine>(verselineDto);

				verseLinesToAdd.Add(verseToAdd);
				noRepeatLineOrder.Add((verselineDto.LyricLineOrder, verselineDto.VerseId));
			}
			
			if (errors.Any()) return BadRequest(errors);
			
			try
			{
				_context.LyricLines.AddRange(verseLinesToAdd);
				await _context.SaveChangesAsync();

				var created = _mapper.Map<List<LyricLineDto>>(verseLinesToAdd);
				createdVerseLineDtos.AddRange(created);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}		

			return Ok(createdVerseLineDtos);
		}

		[HttpPost("create_bridgeline")]
		public async Task<IActionResult> CreateBridgeLine(LyricLineCreateDto bridgeLineDto)
		{
			if (bridgeLineDto == null) return BadRequest("Bridge data is required.");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			if (bridgeLineDto.BridgeId != null)
			{
				var bridgeExists = await _context.Verses
									.AnyAsync(ll => ll.Id == bridgeLineDto.VerseId);

				if (bridgeExists == false)
					return BadRequest($"Parent Bridge Id:{bridgeLineDto.VerseId} does not exist");
			}
			else
			{
				return BadRequest("Bridge Id is required.");
			}

			//No LyricOrderNumber is duplicated within the same bridge
			var bridgeLineExists = await _context.LyricLines
									.Where(vl => vl.BridgeId == bridgeLineDto.BridgeId)// Filter by the same bridge
									.AnyAsync(vl => vl.LyricLineOrder == bridgeLineDto.LyricLineOrder);
			if (bridgeLineExists)
				return Conflict($"Lyric line Order value:{bridgeLineDto.LyricLineOrder} already taken.");			

			var verse = _mapper.Map<LyricLine>(bridgeLineDto);

			_context.LyricLines.Add(verse);
			await _context.SaveChangesAsync();

			var verseDto = _mapper.Map<LyricLineDto>(verse);

			return Ok(verseDto);
		}

		[HttpPost("create_bridgelines")]
		public async Task<IActionResult> CreateBridgeLines(List<LyricLineCreateDto> bridgeLineDtos)
		{
			if (bridgeLineDtos == null || bridgeLineDtos.Count == 0)
				return BadRequest("Bridge line data is required.");

			var bridgeLinesToAdd = new List<LyricLine>();
			var createdBridgeLineDtos = new List<LyricLineDto>();

			var errors = new List<string>();
			var noRepeatLineOrder = new System.Collections.Generic
										.HashSet<(long? LyricOrder, string? BridgeId)>();

			foreach (var bridgeLineDto in bridgeLineDtos)
			{
				if (!TryValidateModel(bridgeLineDto))
				{
					errors.Add($"Invalid data for line number:{bridgeLineDto.LyricLineOrder}");
					continue;
				}

				if (bridgeLineDto.BridgeId != null)
				{
					var bridgeExists = await _context.Bridges
										.AnyAsync(ll => ll.Id == bridgeLineDto.BridgeId);

					if (bridgeExists == false)
					{
						errors.Add($"Parent bridge of Id:{bridgeLineDto.BridgeId} does not exist");
						continue;
					}
				}
				else
				{
					errors.Add("Bridge Id is required.");
					continue;
				}

				//No LyricOrderNumber is duplicated within the same bridge
				var bridgeLineExists = await _context.LyricLines
										.Where(vl => vl.BridgeId == bridgeLineDto.BridgeId)// Filter by the same bridge
										.AnyAsync(vl => vl.LyricLineOrder == bridgeLineDto.LyricLineOrder);

				if (bridgeLineExists)
				{
					errors.Add($"Lyric line Order value:{bridgeLineDto.LyricLineOrder} already taken.");
					continue;
				}

				if (noRepeatLineOrder.Contains((bridgeLineDto.LyricLineOrder, bridgeLineDto.BridgeId)))
				{
					errors.Add($"Invalid! Duplicate line no.{bridgeLineDto.LyricLineOrder} detected for same bridge");
				}

				var verseToAdd = _mapper.Map<LyricLine>(bridgeLineDto);

				bridgeLinesToAdd.Add(verseToAdd);
				noRepeatLineOrder.Add((bridgeLineDto.LyricLineOrder, bridgeLineDto.BridgeId));
			}

			if (errors.Any()) return BadRequest(errors);

			try
			{
				_context.LyricLines.AddRange(bridgeLinesToAdd);
				await _context.SaveChangesAsync();

				var created = _mapper.Map<List<LyricLineDto>>(bridgeLinesToAdd);
				createdBridgeLineDtos.AddRange(created);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return Ok(createdBridgeLineDtos);
		}

		[HttpPost("create_chorusline")]
		public async Task<IActionResult> CreateChorusLine(LyricLineCreateDto chorusLineDto)
		{
			if (chorusLineDto == null) return BadRequest("Chorus data is required.");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			if (chorusLineDto.ChorusId != null)
			{
				var chorusExists = await _context.Verses
									.AnyAsync(ll => ll.Id == chorusLineDto.VerseId);

				if (chorusExists == false)
					return BadRequest($"Parent Chorus Id:{chorusLineDto.VerseId} does not exist");
			}
			else
			{
				return BadRequest("Chorus Id is required.");
			}

			//No LyricOrderNumber is duplicated within the same chorus
			var chorusLineExists = await _context.LyricLines
									.Where(vl => vl.ChorusId == chorusLineDto.ChorusId)// Filter by the same chorus
									.AnyAsync(vl => vl.LyricLineOrder == chorusLineDto.LyricLineOrder);
			if (chorusLineExists)
				return Conflict($"Lyric line Order value:{chorusLineDto.LyricLineOrder} already taken.");

			var verse = _mapper.Map<LyricLine>(chorusLineDto);

			_context.LyricLines.Add(verse);
			await _context.SaveChangesAsync();

			var verseDto = _mapper.Map<LyricLineDto>(verse);

			return Ok(verseDto);
		}

		[HttpPost("create_choruslines")]
		public async Task<IActionResult> CreateChorusLines(List<LyricLineCreateDto> chorusLineDtos)
		{
			if (chorusLineDtos == null || chorusLineDtos.Count == 0)
				return BadRequest("Chorus line data is required.");

			var chorusLinesToAdd = new List<LyricLine>();
			var createdChorusLineDtos = new List<LyricLineDto>();

			var errors = new List<string>();
			var noRepeatLineOrder = new System.Collections.Generic
										.HashSet<(long? LyricOrder, string? ChorusId)>();

			foreach (var chorusLineDto in chorusLineDtos)
			{
				if (!TryValidateModel(chorusLineDto))
				{
					errors.Add($"Invalid data for line number:{chorusLineDto.LyricLineOrder}");
					continue;
				}

				if (chorusLineDto.BridgeId != null)
				{
					var chorusExists = await _context.Choruses
										.AnyAsync(ll => ll.Id == chorusLineDto.ChorusId);

					if (chorusExists == false)
					{
						errors.Add($"Parent chorus of Id:{chorusLineDto.ChorusId} does not exist");
						continue;
					}
				}
				else
				{
					errors.Add("Chorus Id is required.");
					continue;
				}

				//No LyricOrderNumber is duplicated within the same chorus
				var chorusLineExists = await _context.LyricLines
										.Where(vl => vl.ChorusId == chorusLineDto.ChorusId)// Filter by the same chorus
										.AnyAsync(vl => vl.LyricLineOrder == chorusLineDto.LyricLineOrder);

				if (chorusLineExists)
				{
					errors.Add($"Lyric line Order value:{chorusLineDto.LyricLineOrder} already taken.");
					continue;
				}

				if (noRepeatLineOrder.Contains((chorusLineDto.LyricLineOrder, chorusLineDto.ChorusId)))
				{
					errors.Add($"Invalid! Duplicate line no.{chorusLineDto.LyricLineOrder} detected for same bridge");
				}

				var verseToAdd = _mapper.Map<LyricLine>(chorusLineDto);

				chorusLinesToAdd.Add(verseToAdd);
				noRepeatLineOrder.Add((chorusLineDto.LyricLineOrder, chorusLineDto.ChorusId));
			}

			if (errors.Any()) return BadRequest(errors);

			try
			{
				_context.LyricLines.AddRange(chorusLinesToAdd);
				await _context.SaveChangesAsync();

				var created = _mapper.Map<List<LyricLineDto>>(chorusLinesToAdd);
				createdChorusLineDtos.AddRange(created);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return Ok(createdChorusLineDtos);
		}


		[HttpPut("edit_verse/{id}")]
		public async Task<IActionResult> EditVerseLine(string id, LyricLineDto verseLineDto)
		{
			if (verseLineDto == null) return BadRequest("Verse data is required.");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			if (id != verseLineDto.Id)
				return BadRequest($"Lyric lines of Ids:{id} and {verseLineDto.Id} are not the same.");

			//No LyricOrderNumber is duplicated within the same verse
			var verseLineExists = await _context.LyricLines
									.Where(vl => vl.VerseId == verseLineDto.VerseId)// Filter by the same verse
									.AnyAsync(vl => vl.Id!= verseLineDto.Id && // Exclude the current LyricLine being edited
											  vl.LyricLineOrder == verseLineDto.LyricLineOrder);

            if (verseLineExists) 
				return Conflict($"Lyric line Order value:{verseLineDto.LyricLineOrder} already taken.");            

            var verseLineInDb = await _context.LyricLines.FindAsync(id);

			if (verseLineInDb == null) return NotFound($"Lyric Line of ID:{id} does not exist.");

			var verseLine = _mapper.Map(verseLineDto, verseLineInDb);

			if (verseLineDto.VerseId != null)
			{				
				var verseExists = await _context.Verses
									.Where(vl => vl.Id != verseLineDto.Id)
									.AnyAsync(vl=>vl.Id == verseLineDto.VerseId);

				if (verseExists == false)
					return BadRequest($"Parent Verse Id:{verseLineDto.VerseId} does not exist");
			}
			else
			{
				return BadRequest("Verse Id is required.");
			}

			try
			{
				_context.LyricLines.Update(verseLine);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
			
			var editedVerseLine = _mapper.Map<LyricLineDto>(verseLine);

			return Ok(editedVerseLine);
		}

		[HttpPut("edit_bridge/{id}")]
		public async Task<IActionResult> EditBridgeLine(string id, LyricLineDto bridgeLineDto)
		{
			if (bridgeLineDto == null) return BadRequest("Bridge data is required.");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			if (id != bridgeLineDto.Id)
				return BadRequest($"Lyric lines of Ids:{id} and {bridgeLineDto.Id} are not the same.");

			//No LyricOrderNumber is duplicated within the same bridge
			var verseLineExists = await _context.LyricLines
									.Where(vl => vl.VerseId == bridgeLineDto.VerseId)// Filter by the same bridge
									.AnyAsync(vl => vl.Id != bridgeLineDto.Id && // Exclude the current LyricLine being edited
											  vl.LyricLineOrder == bridgeLineDto.LyricLineOrder);

			if (verseLineExists)
				return Conflict($"Lyric line Order value:{bridgeLineDto.LyricLineOrder} already taken.");

			var verseLineInDb = await _context.LyricLines.FindAsync(id);

			if (verseLineInDb == null) return NotFound($"Lyric Line of ID:{id} does not exist.");

			var bridgeLine = _mapper.Map(bridgeLineDto, verseLineInDb);

			if (bridgeLineDto.VerseId != null)
			{
				var bridgeExists = await _context.Verses
									.Where(vl => vl.Id != bridgeLineDto.Id)
									.AnyAsync(vl => vl.Id == bridgeLineDto.VerseId);

				if (bridgeExists == false)
					return BadRequest($"Parent Bridge Id:{bridgeLineDto.VerseId} does not exist");
			}
			else
			{
				return BadRequest("Bridge Id is required.");
			}

			try
			{
				_context.LyricLines.Update(bridgeLine);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			var editedVerseLine = _mapper.Map<LyricLineDto>(bridgeLine);

			return Ok(editedVerseLine);
		}

		[HttpPut("edit_chorus/{id}")]
		public async Task<IActionResult> EditChorusLine(string id, LyricLineDto chorusLineDto)
		{
			if (chorusLineDto == null) return BadRequest("Chorus data is required.");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			if (id != chorusLineDto.Id)
				return BadRequest($"Lyric lines of Ids:{id} and {chorusLineDto.Id} are not the same.");

			//No LyricOrderNumber is duplicated within the same chorus
			var verseLineExists = await _context.LyricLines
									.Where(vl => vl.VerseId == chorusLineDto.VerseId)// Filter by the same chorus
									.AnyAsync(vl => vl.Id != chorusLineDto.Id && // Exclude the current LyricLine being edited
											  vl.LyricLineOrder == chorusLineDto.LyricLineOrder);

			if (verseLineExists)
				return Conflict($"Lyric line Order value:{chorusLineDto.LyricLineOrder} already taken.");

			var verseLineInDb = await _context.LyricLines.FindAsync(id);

			if (verseLineInDb == null) return NotFound($"Lyric Line of ID:{id} does not exist.");

			var chorusLine = _mapper.Map(chorusLineDto, verseLineInDb);

			if (chorusLineDto.VerseId != null)
			{
				var chorusExists = await _context.Verses
									.Where(vl => vl.Id != chorusLineDto.Id)
									.AnyAsync(vl => vl.Id == chorusLineDto.VerseId);

				if (chorusExists == false)
					return BadRequest($"Parent Bridge Id:{chorusLineDto.VerseId} does not exist");
			}
			else
			{
				return BadRequest("Chorus Id is required.");
			}

			try
			{
				_context.LyricLines.Update(chorusLine);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			var editedVerseLine = _mapper.Map<LyricLineDto>(chorusLine);

			return Ok(editedVerseLine);
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			var lyricLine = await _context.LyricLines.FindAsync(id);

			if (lyricLine == null) return NotFound($"Lyric line of ID:{id} does not exist.");

			try
			{
				_context.LyricLines.Remove(lyricLine);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return NoContent();
		}

		[HttpDelete("by_ids")]
		public async Task<IActionResult> DeleteLines(List<string> ids)
		{
			if (ids == null || ids.Count == 0) return BadRequest("LyricLine Ids are required.");

			var lyricLines = await _context.LyricLines.Where(ll => ids.Contains(ll.Id)).ToListAsync();

			if (lyricLines.Any() == false) return NotFound(
				new
				{
					Message = "None of the Provided Ids was found.",
					NotFound = ids
				});

			var foundIds = lyricLines.Select(ll=>ll.Id).ToList();
			var notFoundIds = ids.Except(foundIds).ToList();

			using (var transaction =  await _context.Database.BeginTransactionAsync())
			{
				try
				{
					_context.LyricLines.RemoveRange(lyricLines);

					await _context.SaveChangesAsync();

					await transaction.CommitAsync();
				}
				catch (Exception ex)
				{
					await transaction.RollbackAsync();
					return StatusCode(500, ex.Message);
				}
			}			

			if(lyricLines.Any() == true)
			{
				return Ok( new
				{
					Found = foundIds,
					NotFound = notFoundIds
				});
			}		

			return NoContent();

		}
	}
}
