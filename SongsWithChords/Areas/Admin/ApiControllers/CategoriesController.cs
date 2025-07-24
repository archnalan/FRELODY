using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Areas.Admin.ApiControllers
{
    [Route("api/[controller]")]
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
    }
}