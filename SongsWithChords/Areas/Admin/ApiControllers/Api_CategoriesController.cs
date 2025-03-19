using AutoMapper;
using SongsWithChords.Dtos;
using SongsWithChords.Models;
using LanguageExt.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
using System.Linq;
using System.Web.Http.ModelBinding;
using SongsWithChords.Data.Infrastructure;

namespace SongsWithChords.Areas.Admin.ApiControllers
{
    [Route("admin/[controller]")]
	[ApiController]
	[Area("Admin")]
	public class Api_CategoriesController : ControllerBase
	{
		private readonly SongDbContext _context;
		private readonly IMapper _mapper;

        public Api_CategoriesController(SongDbContext context, IMapper mapper)
        {
            _context = context;
			_mapper = mapper;
        }

		//GET admin/api_categories
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var categories = await _context.Categories
									.OrderBy(c => c.Sorting)
									.ToListAsync();
			var categoryDtos = categories.Select(_mapper.Map<Category, CategoryDto>).ToList();

			return Ok(categoryDtos);
		}

		//GET admin/api_categories/{id}
		[HttpGet("{id}")]
		public async Task<IActionResult> GetCategoryByID(int id)
		{			
			var category = await _context.Categories.FindAsync(id);

			if (category == null) return BadRequest($"Category of ID: {id} does not exist");

			var categoryDto = _mapper.Map<Category, CategoryDto>(category);

			return Ok(categoryDto);
		}

		//GET admin/api_categories/by_ids
		[HttpGet("by_ids")]
		public async Task<IActionResult> GetCategoriesByID(Guid[] ids)
		{
			//Find match in Db
			var categories = await _context.Categories
								.Where(c => ids.Contains(c.Id))
								.ToListAsync();

			//var categoryDto = categories.Select(_mapper.Map<Category, CategoryDto>);

			var categoryDtos = _mapper.Map<List<CategoryDto>>(categories);

			var foundIds = categories.Select(c => c.Id).ToList();
			var notFoundIds = ids.Except(foundIds).ToList();

			if (notFoundIds.Count == ids.Length)
				return NotFound(new { NotFound = notFoundIds });

			if (notFoundIds.Any())
			{
				return Ok(new {
					 Found = categoryDtos,
					 NotFound = notFoundIds
				});
			}
			return Ok(categoryDtos);
		}

		//POST admin/api_categories/create

		[HttpPost]
		[Route("create")]
		public async Task<IActionResult> Create(CategoryDto categoryDto)
		{
			if (categoryDto == null) return BadRequest("Category Data is required");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			categoryDto.CategorySlug = categoryDto.Name.ToLower().Replace(" ", "-");
			categoryDto.Sorting = 100;

			var slugExists = await _context.Categories
				.AnyAsync(c => c.CategorySlug == categoryDto.CategorySlug);

			if (slugExists) return Conflict($"Category: {categoryDto.Name} already Exists.");

			var category = _mapper.Map<CategoryDto, Category>(categoryDto);

			await _context.Categories.AddAsync(category);
			await _context.SaveChangesAsync();

			var newCategory = _mapper.Map<Category, CategoryDto>(category);

			return CreatedAtAction(nameof(GetCategoryByID), new { id = category.Id}, newCategory);
		}

		//POST admin/api_categories/create_many
		[HttpPost]
		[Route("create_many")]
		public async Task<IActionResult> CreateMany([FromBody]CategoryDto[] categoryDtos)
		{
			if (categoryDtos == null || categoryDtos.Length == 0) 
				return BadRequest("Category Data is required");

			var categoriesToAdd = new List<Category>();
			var createdCategories = new List<CategoryDto>();
			 
			var errors = new List<string>();
			var readyCategories = new HashSet<(string name, Guid? parentCategoryId)>();

			foreach(var categoryDto in categoryDtos)
			{
				if (!TryValidateModel(categoryDto))
				{
					errors.Add($"Invalid data for category: {categoryDto.Name}");
					continue;
				}				

				categoryDto.CategorySlug = categoryDto.Name.ToLower().Replace(" ", "-");
				categoryDto.Sorting = 100;

				var slugExists = await _context.Categories
					.AnyAsync(c => c.CategorySlug == categoryDto.CategorySlug);

				if (slugExists)
				{
					errors.Add($"Category: {categoryDto.Name} Already Exists.");
					continue;
				}

				if(readyCategories.Contains((categoryDto.Name, categoryDto.ParentCategoryId)))
				{
					errors.Add($"Invalid Attempt! Duplicate category:{categoryDto.Name} detected.");
					continue;
				}

				var category = _mapper.Map<CategoryDto, Category>(categoryDto);

				await _context.Categories.AddAsync(category);

				categoriesToAdd.Add(category);//store results for later after saving				
				readyCategories.Add((category.Name, category.ParentCategoryId));
			}
			try			
			{				
				await _context.SaveChangesAsync();
				
				foreach(var category in categoriesToAdd)
				{
					var newCategoryDto = _mapper.Map<Category, CategoryDto>(category);
					
					createdCategories.Add(newCategoryDto);//these will have have correct Ids 
				}
			}
			catch (Exception ex)
			{				
				errors.Add($"{ex.Message}");
			}

			if (errors.Any())
			{
				return Ok(new 
				{ 
					Created = createdCategories,
					Errors = errors
				});
				
			}

			return Ok(createdCategories);
		}

		//PUT admin/api_categories/edit_many
		[HttpPut]
		[Route("edit_many")]
		public async Task<IActionResult> EditMany(CategoryDto[] categoryDtos)
		{
			if (categoryDtos == null || categoryDtos.Length == 0)
				return BadRequest("Category Data is required");

			var updatedCategories = new List<CategoryDto>();
			var notFoundResult = new List<string>();
			var conflictResult = new List<string>();

			foreach (var categoryDto in categoryDtos)
			{
				var editResult= await EditCategory(categoryDto.Id, categoryDto);//Edit happens in here

				if (editResult is NotFoundObjectResult)				
								notFoundResult.Add($"Category with ID: {categoryDto.Id} does not exist.");
				
				if(editResult is ConflictObjectResult)				
								conflictResult.Add($"Category: {categoryDto.Name} Already Exists.");
				
				//if(editResult is CategoryDto updatedCategoryDto)		//Won't succed coz (editResult is Ok here)
				//			updatedCategories.Add(updatedCategoryDto);

				if(editResult is OkObjectResult Okresult && Okresult.Value is CategoryDto updatedCatDto)
					updatedCategories.Add(updatedCatDto);			

			}

			await _context.SaveChangesAsync();

			//crafting response
			var response = new
			{
				Updated = updatedCategories,
				NotFound = notFoundResult,
				Conflict = conflictResult
			};

			if (notFoundResult.Any() || conflictResult.Any())
			{
				return BadRequest(response);
			}

			return Ok(response.Updated);
		}

		//PUT admin/api_categories/edit
		[HttpPut]
		[Route("edit/{id}")]
		public async Task<IActionResult> Edit(Guid id, CategoryDto categoryDto)
		{
			if (categoryDto == null) return BadRequest("Category Data is required");

			var editResult = await EditCategory(id, categoryDto); //Edit happens in here

			if (editResult == null) return BadRequest("Catagery Data could not be processed.");

			//Waits to implement logic for unnecessary updates
			//if (editResult is OkObjectResult okObject) return Ok(okObject.Value);

			if (editResult is IActionResult result) return Ok(result);		

			return StatusCode(StatusCodes.Status500InternalServerError,"An Unexpected Error Occured.");
		}

		public async Task<IActionResult> EditCategory(Guid id, CategoryDto categoryDto)
		{
			if (categoryDto == null) return BadRequest("Category Data is required");

			if (!ModelState.IsValid) return BadRequest(ModelState);

			var categoryInDb = await _context.Categories.FindAsync(id);

			if (categoryInDb == null)
				return NotFound($"The category with ID: {id} does not exist.");

			categoryDto.CategorySlug = categoryDto.Name.ToLower().Replace(" ", "-");
			categoryDto.Sorting = 100;

			var slugExists = await _context.Categories
				.Where(s => s.Id != id) // because the database already has slug for this model
				.AnyAsync(c => c.CategorySlug == categoryDto.CategorySlug);

			if (slugExists) return Conflict("Category Already Exists.");

			//To avoid uneccessary Update

			/*if (categoryInDb.Name == categoryDto.Name &&
			   categoryInDb.CategorySlug == categoryDto.CategorySlug &&
			   categoryInDb.Sorting == categoryDto.Sorting)
							return Ok(new { Message = $"Category: {categoryDto.Name} is up to date", 
										  Category = categoryDto });*/  

			var category = _mapper.Map(categoryDto, categoryInDb);

			_context.Categories.Update(category);

			await _context.SaveChangesAsync();

			var updatedCategoryDto = _mapper.Map<Category, CategoryDto>(categoryInDb);

			return Ok(updatedCategoryDto);
		}
		

		//DELETE admin/api_categories/{id}
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			var category = await _context.Categories.FindAsync(id);

			if (category == null) 
				return NotFound($"The category with ID: {id} does not exist.");
			try
			{
				_context.Categories.Remove(category);

				await _context.SaveChangesAsync();
			}
			catch(DbUpdateException ex)
			{
				if(ex.InnerException is 
					Microsoft.Data.SqlClient.SqlException sqlEx 
					&& sqlEx.Number ==547)
					return BadRequest(sqlEx.Message);

				return BadRequest($"An Error occured on attempt to delete category: {ex.Message}");
			}
			catch(Exception ex)
			{
				return BadRequest($"An Error occured on attempt to delete category: {ex.Message}");
			}
			
			return NoContent();
		}

		//POST admin/api_categories/re-assign
		//method that takes an array of categoryIds and assigns it to a new category Id	

    }
}
