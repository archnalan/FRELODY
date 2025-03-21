using AutoMapper;
using DocumentFormat.OpenXml.Office.CoverPageProps;
using FRELODYAPP.Dtos;
using FRELODYAPP.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FRELODYAPP.Data.Infrastructure;

namespace FRELODYAPP.Areas.Admin.ApiControllers
{
    [Route("admin/[controller]")]
	[ApiController]
	[Area("admin")]
	public class Api_SongsController : ControllerBase
	{
		private readonly SongDbContext _context;
		private readonly IMapper _mapper;

        public Api_SongsController(SongDbContext context, IMapper mapper)
        {
			_context = context;
			_mapper = mapper;
		}

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var Songs = await _context.Songs
						.OrderBy(s => s.SongNumber)
						.ToListAsync();		

			var SongsDto = _mapper.Map<List<SongDto>>(Songs);

			return Ok(SongsDto);
		}

		[HttpGet("categories")]
		public async Task<IActionResult> GetSongsWithCategories()
		{
			var Songs = await _context.Songs
						.OrderBy(h => h.SongNumber)
						.ToListAsync();		

			var SongDtos = _mapper.Map<List<SongDto>>(Songs);

			return Ok(SongDtos);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetSongById(Guid id)
		{
			var Song = await _context.Songs.FindAsync(id);

			if(Song == null) return NotFound($"Song of ID:{id} does not exist.");

			var SongDto = _mapper.Map<SongDto>(Song);

			return Ok(SongDto);
		}
		[HttpGet("category/{id}")]
		public async Task<IActionResult> GetSongWithCategoryById(Guid id)
		{
			var Song = await _context.Songs
							.FirstOrDefaultAsync(h=>h.Id == id);

			if(Song == null) return NotFound($"Song of ID:{id} does not exist.");

			var SongDto = _mapper.Map<SongDto>(Song);

			return Ok(SongDto);
		}

		[HttpGet("by_ids")]
		public async Task<IActionResult> GetSongsByIds(List<Guid> ids)
		{
			if (ids == null || ids.Count == 0) return BadRequest("Song Ids are required.");
			
			var Songs = await _context.Songs
						.Where(h=>ids.Contains(h.Id))
						.ToListAsync();

			var foundSongsDtos = _mapper.Map<List<SongDto>>(Songs);

			var notFoundSongsDtos = ids.Except(foundSongsDtos.Select(h => h.Id)).ToList();

			if(notFoundSongsDtos.Count == ids.Count) return NotFound(notFoundSongsDtos);

			if (notFoundSongsDtos.Any())
			{
				return Ok(new
				{
					Found = foundSongsDtos,
					NotFound = notFoundSongsDtos
				});
			}

			return Ok(foundSongsDtos);
		}

		[HttpPost("create")]
		public async Task<IActionResult> Create(SongCreateDto createDto)
		{
			if (createDto == null) return BadRequest("Song data is required.");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			createDto.Slug = createDto.Title.ToLower().Replace(" ", "-");

			var SongExists = await _context.Songs.AnyAsync(hE=>hE.Slug ==  createDto.Slug);

			if (SongExists) return Conflict($"Song: {createDto.Title} already exists.");
			
			var Song = _mapper.Map<Song>(createDto);

			try
			{				
				await _context.Songs.AddAsync(Song);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			var createdSongDto = _mapper.Map<SongDto>(Song);

			return CreatedAtAction(nameof(GetSongById), new {id = Song.Id}, createdSongDto);
		}

		[HttpPut("edit/{id}")]
		public async Task<IActionResult> Edit(Guid id, SongDto SongDto)
		{
			if (SongDto == null) return BadRequest("Song Data is required");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			if (id != SongDto.Id) 
				return BadRequest($"Invalid Attempt! Songs with Ids {id} and {SongDto.Id} are not the same.");

			var SongInDb = await _context.Songs.FindAsync(id);

			if (SongInDb == null) return NotFound($"Song with ID: {id} does not exist.");

			SongDto.Slug = SongDto.Title.ToLower().Replace(" ", "-");

			var SongExists = await _context.Songs
								.Where(hE=>hE.Id != id)
								.AnyAsync(hE => hE.Slug == SongDto.Slug);

			if (SongExists) return Conflict($"Song: {SongDto.Title} already exists.");

			var categoryExists = await _context.Categories
									.FirstOrDefaultAsync(hC => hC.Id == SongDto.CategoryId);

			if (categoryExists == null)
				return BadRequest($"Song Category of ID: {SongDto.CategoryId} does not exist.");

            try
            {
                // Map updated values from SongDto to SongInDb
                SongInDb.SongNumber = SongDto.SongNumber;
                SongInDb.Title = SongDto.Title ?? SongInDb.Title;
                SongInDb.Slug = SongDto.Slug;
                SongInDb.WrittenDateRange = SongDto.WrittenDateRange ?? SongInDb.WrittenDateRange;
                SongInDb.WrittenBy = SongDto.WrittenBy ?? SongInDb.WrittenBy;
                SongInDb.History = SongDto.History ?? SongInDb.History;
                SongInDb.CategoryId = SongDto.CategoryId;

                _context.Songs.Update(SongInDb);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            var editedSongDto = _mapper.Map<SongDto>(SongInDb);

			return Ok(editedSongDto);
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			var Song = await _context.Songs.FindAsync(id);

			if (Song == null) return NotFound($"Song with ID:{id} does not exist.");

			try
			{
				_context.Songs.Remove(Song);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return NoContent();
		}

		//DELETE admin/api_Songs/by_ids
		[HttpDelete("by_ids")]
		public async Task<IActionResult> DeleteSongs(List<int> ids)
		{
			if (ids == null || ids.Count == 0) return BadRequest("Song Ids are required.");

			var deletedIds = new List<int>();
			var errors = new List<string>();

			foreach (int id in ids)
			{
				var Song = await _context.Songs.FindAsync(id);

				if (Song == null)
				{
					errors.Add($"Song with ID: {id} does not exist.");
					continue;
				}
				_context.Songs.Remove(Song);
				deletedIds.Add(id);
			}
			try
			{
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				errors.Add(ex.Message);
			}

			if (errors.Count == ids.Count) return NotFound(errors);

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
