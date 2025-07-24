using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos;
using FRELODYAPP.Models;
using FRELODYAPP.ServiceHandler;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class CategoryService : ICategoryService
    {
        private readonly SongDbContext _context;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(SongDbContext context, ILogger<CategoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Get all categories
        public async Task<ServiceResult<List<CategoryDto>>> GetAllCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .OrderBy(c => c.Sorting)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                var categoriesDto = categories.Adapt<List<CategoryDto>>();

                return ServiceResult<List<CategoryDto>>.Success(categoriesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while retrieving categories. {Error}", ex);
                return ServiceResult<List<CategoryDto>>.Failure(
                    new ServerErrorException("An error occurred while retrieving categories."));
            }
        }
        #endregion
    }
}