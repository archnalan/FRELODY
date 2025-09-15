using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<CategoryDto>), 200)]
        public async Task<IActionResult> GetAllCategories()
        {
            var result = await _categoryService.GetAllCategories();

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<CategoryDto>), 200)]
        public async Task<IActionResult> GetCategoriesBySongBookId([FromQuery]string songBookId)
        {
            var result = await _categoryService.GetCategoriesBySongBookId(songBookId);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<SongDto>), 200)]
        public async Task<IActionResult> GetAllSongsByCategoryId([FromQuery]string categoryId)
        {
            var result = await _categoryService.GetAllSongsByCategoryId(categoryId);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(CategoryDto), 200)]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryDto categoryDto)
        {
            var result = await _categoryService.CreateCategory(categoryDto);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPut]
        [ProducesResponseType(typeof(CategoryDto), 200)]
        public async Task<IActionResult> UpdateCategory([FromBody] CategoryDto categoryDto)
        {
            var result = await _categoryService.UpdateCategory(categoryDto.Id, categoryDto);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }
    }
}