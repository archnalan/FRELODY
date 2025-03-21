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
	public class Api_PagesController : ControllerBase
	{
		private readonly SongDbContext _context;
		private readonly IMapper _mapper;

		public Api_PagesController(SongDbContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
		}

		//GET admin/api_pages
		[HttpGet]
		public async Task<ActionResult<IEnumerable<PageDto>>> Index()
		{
			var pages = await _context.Pages
								.OrderBy(s => s.Sorting)
								.ToListAsync();
			var pageDtos = pages.Select(_mapper.Map<Page, PageDto>);

			return Ok(pageDtos);
		}

		//GET aadmin/api_pages/1
		[HttpGet("{id}")]
		public async Task<ActionResult<PageDto>> GetPageById(int id)
		{
			var page = await _context.Pages.FindAsync(id);

			if (page == null) return NotFound($"The Page with ID: {id} does not exist.");

			var pageDto = _mapper.Map<Page, PageDto>(page);

			return Ok(pageDto);
		}

		//POST admin/apipages/create
		[HttpPost]
		[Route("create")]
		public async Task<IActionResult> Create([FromBody] PageDto pageDto)
		{
			if (pageDto == null) return BadRequest("Page Data is required");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			pageDto.Slug = pageDto.Title.ToLower().Replace(" ", "-");
			pageDto.Sorting = 100;

			var pageExists = await _context.Pages.AnyAsync(s => s.Slug == pageDto.Slug);

			if (pageExists) return Conflict($"The Page {pageDto.Title} Already Exists.");

			var page = _mapper.Map<PageDto, Page>(pageDto);

			await _context.Pages.AddAsync(page);
			await _context.SaveChangesAsync();

			var newPageDto = _mapper.Map<PageDto>(page);

			return CreatedAtAction(nameof(GetPageById), new { id = page.Id }, newPageDto);
		}

		//PUT admin/apipages/edit/"{id}"
		[HttpPut("edit/{id}")]
		public async Task<IActionResult> Edit([FromRoute] int id, [FromBody] PageDto pageDto)
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var pageInDb = await _context.Pages.FindAsync(id);

			if (pageInDb == null)
				return NotFound($"The Page with ID: {id} was not found");


			if (pageDto.Id == 1)
			{
				pageDto.Slug = "home";
			}
			else
			{
				pageDto.Slug = pageDto.Title
								.ToLower()
								.Replace(" ", "-");
			}

			pageDto.Sorting = 100;

			var slug = await _context.Pages
								.Where(x => x.Id != id)
								.FirstOrDefaultAsync(s => s.Slug == pageDto.Slug);

			if (slug != null) return Conflict($"The Page: {pageDto.Title} Already Exists.");

			var page = _mapper.Map(pageDto, pageInDb);

			_context.Pages.Update(page);

			await _context.SaveChangesAsync();

			var updatedPageDto = _mapper.Map<Page, PageDto>(page);

			return Ok(updatedPageDto);
		}

		//DELETE admin/apipages/"{id}"
		[HttpDelete("{id}")]		
		public async Task<IActionResult> Delete(int id)
		{
			var pageInDb = await _context.Pages.FindAsync(id);

			if (pageInDb == null)
				return NotFound($"The Page with ID: {id} was not found");

			_context.Remove(pageInDb);
			await _context.SaveChangesAsync();

			return NoContent();
		}

		//POST admin/apipages/reorder/
		[HttpPost]
		public async Task<IActionResult> Reorder(int[] id)
		{
			int count = 1;

			foreach (var pageId in id)
			{
				Page page = await _context.Pages.FindAsync(pageId);

				if (page == null)
					return NotFound($"The Page with ID: {id} was not found.");

				page.Sorting = count;

				_context.Pages.Update(page);

				count++;
			}
			await _context.SaveChangesAsync();

			return Ok();
		}
	}
}
