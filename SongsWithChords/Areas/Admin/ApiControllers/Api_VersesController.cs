using AutoMapper;
using SongsWithChords.Dtos;
using SongsWithChords.Models;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using SongsWithChords.Data.Infrastructure;

namespace SongsWithChords.Areas.Admin.ApiControllers
{
    [Route("admin/[controller]")]
	[ApiController]
	[Area("admin")]
	public class Api_VersesController : ControllerBase
	{
		private readonly SongDbContext _context;
		private readonly IMapper _mapper;

		public Api_VersesController(SongDbContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
		}

		//GET admin/api_verses
		
		public async Task<IActionResult> Index()
		{
			var verses = await _context.Verses
							.OrderBy(v=>v.VerseNumber)
							.ToListAsync();

			var versesDto = _mapper.Map<List<VerseDto>>(verses).ToList();

			return Ok(versesDto);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetVerseById(Guid id)
		{
			var verse = await _context.Verses.FindAsync(id);

			if (verse == null) return NotFound($"Verse with ID:{id} does not exist.");

			var verseDto = _mapper.Map<VerseDto>(verse);

			return Ok(verseDto);
		}

		[HttpGet("by_ids")]
		public async Task<IActionResult> GetVersesByIds(List<Guid> Ids)
		{
			if (Ids == null || Ids.Count == 0) return BadRequest("Verse Ids are required.");

			var verses = await _context.Verses
							.Where(v => Ids.Contains(v.Id))
							.ToListAsync();

			var foundVersesDto = _mapper.Map<List<VerseDto>>(verses);

			var notFoundVersesDto = Ids.Except(foundVersesDto
									   .Select(v=>v.Id ?? Guid.Empty)).ToList();

			if (notFoundVersesDto.Count == Ids.Count) 
								return NotFound(notFoundVersesDto);

			if (notFoundVersesDto.Any())
			{
				return Ok(new 
				{
					Found = foundVersesDto,
					NotFound = notFoundVersesDto
				});
			}

			return Ok(foundVersesDto);

		}

		[HttpPost("create")]
		public async Task<IActionResult> Create(VerseCreateDto verseDto)
		{
			if (verseDto == null) return BadRequest("Verse data is required.");

			if(!ModelState.IsValid) return BadRequest(ModelState);

			var SongInDb = await _context.Songs.FindAsync(verseDto.SongId);

			if (SongInDb == null)
				return BadRequest($"Parent Song with ID: {verseDto.SongId} does not exist");

			var verseExists = await _context.Verses
								.Where(v=>v.SongId == verseDto.SongId)
								.AnyAsync(v=>v.VerseNumber == verseDto.VerseNumber);

			if (verseExists) 
				return Conflict($"Verse Number {verseDto.VerseNumber} already exists for this Song");

			var verse = _mapper.Map<Verse>(verseDto);
			try
			{
				await _context.Verses.AddAsync(verse);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			var newVerseDto = _mapper.Map<VerseDto>(verse);

			return CreatedAtAction(nameof(GetVerseById), new { id = verse.Id}, newVerseDto);
		}

		[HttpPost("create_many")]
		public async Task<IActionResult> CreateVerses(List<VerseCreateDto> verseCreateDtos)
		{
			if (verseCreateDtos == null || verseCreateDtos.Count == 0)
				return BadRequest("Verse data is required.");

			var versesToAdd = new List<Verse>();

			var createdVersesDto = new List<VerseDto>();

			var errors = new List<string>();

			foreach(var verseDto in verseCreateDtos)
			{
				if (!TryValidateModel(verseDto))
				{
					errors.Add($"Invalid data for {verseDto.VerseNumber}");
					continue;
				}

				var SongInDb = await _context.Songs.FindAsync(verseDto.SongId);

				if (SongInDb == null)
				{
					errors.Add($"Parent Song with ID: {verseDto.SongId} does not exist");
					continue;
				}				

				var verseExists = await _context.Verses
								.Where(v => v.SongId == verseDto.SongId)
								.AnyAsync(v => v.VerseNumber == verseDto.VerseNumber);

				if (verseExists)
				{
					errors.Add($"Verse Number {verseDto.VerseNumber} already exists for this Song");
					continue;
				}

				var verse = _mapper.Map<Verse>(verseDto);

				versesToAdd.Add(verse);
			}

			if (errors.Any()) return BadRequest(errors);

			if (versesToAdd.Count > 0)
			{
				try
				{
					await _context.Verses.AddRangeAsync(versesToAdd);

					await _context.SaveChangesAsync();

					foreach (var verse in versesToAdd)
					{
						var newVerseDto = _mapper.Map<VerseDto>(verse);
						createdVersesDto.Add(newVerseDto);
					}

				}
				catch (Exception ex)
				{
					return BadRequest(ex.Message);
				}
			}

			return Ok(createdVersesDto);
		}

		[HttpPut("edit/{id}")]
		public async Task<IActionResult> Edit(Guid id, VerseDto verseEdit)
		{
			if (verseEdit == null) return BadRequest("Verse data is required.");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			if (id != verseEdit.Id) 
				return BadRequest($"Invalid Attempt! Verses of IDs:{id} and {verseEdit.Id} are not the same");

			var verseExists = await _context.Verses
								.Where(v=>v.Id != id)															
								.AnyAsync(v=>v.SongId == verseEdit.SongId && v.VerseNumber == verseEdit.VerseNumber);

			if (verseExists)
			{
				return Conflict($"Verse Number {verseEdit.VerseNumber} already exists for this Song");				
			}

			var verseInDb = await _context.Verses.FindAsync(id);

			if (verseInDb == null) 
				return BadRequest($"Verse with ID: {id} does not exist");

			var SongInDb = await _context.Songs.FindAsync(verseEdit.SongId);

			if (SongInDb == null)
				return BadRequest($"Parent Song with ID: {verseEdit.SongId} does not exist");

			var verse = _mapper.Map(verseEdit, verseInDb);

			try
			{
				_context.Verses.Update(verse);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return BadRequest(ex);
			}

			var newVerseDto = _mapper.Map<VerseDto>(verse);

			return Ok(newVerseDto);

		}

		[HttpPut("edit_many")]
		public async Task<IActionResult> EditVerses(List<VerseDto> verseDtos)
		{
			if (verseDtos == null || verseDtos.Count == 0) 
				return BadRequest("Verse data is required.");

			var versesToEdit = new List<Verse>();
			var editedVerseDtos = new List<VerseDto>();

			var errors = new List<string>();
			var processedVerses = new HashSet<(Guid SongId, int  Number)>();

			foreach(var verseDto in verseDtos)
			{
				if (!TryValidateModel(verseDto))
				{
					errors.Add($"Invalid data for Verse {verseDto.VerseNumber}");
					continue;
				}
				var SongInDb = await _context.Songs.FindAsync(verseDto.SongId);

				if(SongInDb == null)
				{
					errors.Add($"Parent Song with ID:{verseDto.SongId} does not exist.");
					continue;
				}
				var verseExists = await _context.Verses
									.Where(v=>v.SongId == verseDto.SongId)
									.AnyAsync(v=>v.Id != verseDto.Id && v.VerseNumber == verseDto.VerseNumber);
				if (verseExists)
				{
					errors.Add($"Verse Number {verseDto.VerseNumber} already exists for this Song");
					continue;
				}

				var verseInDb = await _context.Verses.FindAsync(verseDto.Id);

				if(verseInDb == null)
				{
					errors.Add($"Verse with ID:{verseDto.Id} does not exist.");
					continue;
				}

				if(processedVerses.Contains((verseDto.SongId, verseDto.VerseNumber)))
				{
					errors.Add($"Invalid Attempt! Duplicate Verse Number:{verseDto.VerseNumber} for same Song.");
					continue;
				}
				var verse = _mapper.Map(verseDto, verseInDb);

				versesToEdit.Add(verse);
				processedVerses.Add((verseDto.SongId, verseDto.VerseNumber));
			}
			if (errors.Count > 0) return BadRequest(errors);
			if(versesToEdit.Count > 0)
			{
				try
				{
					//Stop Entry of adding same Number property to vesesTo Edit
					_context.Verses.UpdateRange(versesToEdit);
					await _context.SaveChangesAsync();

					editedVerseDtos = _mapper.Map<List<VerseDto>>(versesToEdit);
				}
				catch (Exception ex)
				{
					return BadRequest(ex.Message);
				}
			}
			return Ok(editedVerseDtos);
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			var verse = await _context.Verses.FindAsync(id);

			if (verse == null) return NotFound($"Verse with ID:{id} does not exist.");

			try
			{
				_context.Verses.Remove(verse);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return BadRequest(ex);
			}

			return NoContent();
		}

		[HttpDelete("by_ids")]
		public async Task<IActionResult> DeleteVerses(List<Guid> Ids)
		{
			if (Ids.Count == 0) return BadRequest("Verse Ids are required.");

			var deletedIds = new List<Guid>();
			var errors = new List<string>();

			foreach (Guid Id in Ids)
			{
				var verse = await _context.Songs.FindAsync(Id);

				if (verse == null)
				{
					errors.Add($"Verse with ID: {Id} does not exist.");
					continue;
				}
				_context.Songs.Remove(verse);
				deletedIds.Add(Id);
			}
			try
			{
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				errors.Add(ex.Message);
			}

			if (errors.Count == Ids.Count) return NotFound(errors);

			if (errors.Any())
			{
				return Ok(new
				{
					Deleted = deletedIds,
					NotDeleted = errors
				});
			}

			return NoContent();
		}

	}
}
