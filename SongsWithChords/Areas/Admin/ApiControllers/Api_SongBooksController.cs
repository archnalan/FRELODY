using AutoMapper;
using FRELODYAPP.Dtos;
using FRELODYAPP.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FRELODYAPP.Data.Infrastructure;

namespace FRELODYAPP.Areas.Admin.ApiControllers
{
    [Route("admin/[controller]")]
	[ApiController]
	[Area("Admin")]
	public class Api_SongBooksController : ControllerBase
	{
		private readonly SongDbContext _context;
		private readonly IMapper _mapper;

        public Api_SongBooksController(SongDbContext context, IMapper mapper)
        {
            _context = context;
			_mapper = mapper;
        }

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var SongBooks = await _context.SongBooks
								.OrderBy(hb=>hb.Title)
								.ToListAsync();
			var hymBookDtos = _mapper.Map<List<SongBookDto>>(SongBooks);

			return Ok(hymBookDtos);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetSongBookById(int id)
		{
			var SongBook = await _context.SongBooks.FindAsync(id);

			if (SongBook == null) return NotFound($"SongBook with ID: {id} does not exist.");

			var SongBookDto = _mapper.Map<SongBookDto>(SongBook);

			return Ok(SongBookDto);
		}
		
		[HttpGet("book_categories/{id}")]
		public async Task<IActionResult> GetSongBookWithCategories(Guid id)
		{
			var SongBook = await _context.SongBooks
							.Include(hb=>hb.Categories)
							.FirstOrDefaultAsync(hb=>hb.Id == id);

			if (SongBook == null) return NotFound($"SongBook with ID: {id} does not exist.");

			var SongBookDto = _mapper.Map<SongBookWithCategoriesDto>(SongBook);

			return Ok(SongBookDto);
		}

		[HttpGet("by_ids")]
		public async Task<IActionResult> GetSongBooksByIds(List<Guid> ids)
		{
			if (ids == null || ids.Count == 0) 
				return BadRequest("A List of Song Book Ids is required");

			var SongBooks = await _context.SongBooks
							.Where(hb=>ids.Contains(hb.Id))
							.ToListAsync();

			var foundBooksDto = _mapper.Map<List<SongBookDto>>(SongBooks);

			var notFoundBooksDto = ids.Except(foundBooksDto.Select(hb=>hb.Id)).ToList();

			if(notFoundBooksDto.Count == ids.Count) return NotFound(notFoundBooksDto);

			if (notFoundBooksDto.Any())
			{
				return Ok(new
				{
					Found = foundBooksDto,
					NotFound = notFoundBooksDto
				});
			}

			return Ok(foundBooksDto);
		}

		[HttpPost("create")]
		public async Task<IActionResult> Create(SongBookCreateDto bookCreateDto)
		{
			if (bookCreateDto == null) return BadRequest("Song Book data is required.");

			if(!ModelState.IsValid) return BadRequest(ModelState);

			var bookExists = await _context.SongBooks
								.AnyAsync(hb=>hb.Title == bookCreateDto.Title);

			if (bookExists) return Conflict($"Song Book: {bookCreateDto.Title} already Exists.");

			bookCreateDto.Slug = bookCreateDto.Title.ToLower().Replace(" ", "-");

			var createBook = _mapper.Map<SongBookCreateDto, SongBook>(bookCreateDto);

			try
			{
				await _context.SongBooks.AddAsync(createBook);
				await _context.SaveChangesAsync();
			}
			catch(Exception ex)
			{
				return BadRequest(ex.Message);
			}

			var newBook = _mapper.Map<SongBookDto>(createBook);

			return CreatedAtAction(nameof(GetSongBookById), new { id = createBook.Id }, newBook);
		}

		[HttpPut("edit/{id}")]
		public async Task<IActionResult> Edit(Guid id, SongBookDto bookDto)
		{

			if (bookDto == null) return BadRequest("Song Book data is required");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			if (id != bookDto.Id) 
				return BadRequest($"Invalid Attempt! Song Books of Ids {id} and {bookDto.Id} are not the same");

			var bookInDb = await _context.SongBooks.FindAsync(id);

			if (bookInDb == null) return NotFound($"Song Book with ID:{id} does not exist.");

			bookDto.Slug = bookDto.Title.ToLower().Replace(" ", "-");

			var bookExists = await _context.SongBooks
								.Where(cb => cb.Id != id)
								.AnyAsync(cb=>cb.Slug == bookDto.Slug);

			if (bookExists) return Conflict($"Song Book: {bookDto.Title} already exists.");

			var editedBook = _mapper.Map(bookDto, bookInDb);

			try
			{
				_context.SongBooks.Update(editedBook);
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			var editedBookDto = _mapper.Map<SongBookDto>(editedBook);

			return Ok(editedBookDto);

		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			var SongBook = await _context.SongBooks.FindAsync(id);

			if (SongBook == null) return NotFound($"Song Book with ID:{id} does not exist.");

			try
			{
				_context.SongBooks.Remove(SongBook);
				await _context.SaveChangesAsync();
			}
			catch(Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return NoContent();
		}

		//DELETE admin/api_Songbooks/by_ids
		[HttpDelete("by_ids")]
		public async Task<IActionResult> DeleteSongBooks(List<Guid> ids)
		{
			if (ids == null || ids.Count == 0) return BadRequest("Song Book Ids are required.");

			var deletedIds = new List<Guid>();
			var errors = new List<string>();

			foreach (Guid id in ids)
			{
				var SongBook = await _context.SongBooks.FindAsync(id);

				if (SongBook == null)
				{
					errors.Add($"Chart with ID: {id} does not exist.");
					continue;
				}
				_context.SongBooks.Remove(SongBook);
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
