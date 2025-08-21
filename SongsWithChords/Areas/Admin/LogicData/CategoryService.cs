using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos;
using FRELODYAPP.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
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

        #region Get categories by song book Id
        public async Task<ServiceResult<List<CategoryDto>>> GetCategoriesBySongBookId(string songBookId)
        {
            try
            {
                if (string.IsNullOrEmpty(songBookId))
                {
                    return ServiceResult<List<CategoryDto>>.Failure(
                        new BadRequestException("Song book ID is required"));
                }
                var categories = await _context.Categories
                    .Where(c => c.SongBookId == songBookId)
                    .OrderBy(c => c.Sorting)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
                var categoriesDto = categories.Adapt<List<CategoryDto>>();
                return ServiceResult<List<CategoryDto>>.Success(categoriesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while retrieving categories for song book {SongBookId}. {Error}", songBookId, ex);
                return ServiceResult<List<CategoryDto>>.Failure(
                    new ServerErrorException("An error occurred while retrieving categories for the specified song book."));
            }
        }
        #endregion

        #region Get all songs by category
        public async Task<ServiceResult<List<SongDto>>> GetAllSongsByCategoryId(string categoryId)
        {
            try
            {
                if(string.IsNullOrEmpty(categoryId))
                {
                    return ServiceResult<List<SongDto>>.Failure(
                        new BadRequestException("Category ID is required"));
                }
                var songs = await _context.Songs
                    .Where(s => s.CategoryId == categoryId)
                    .OrderBy(s => s.SongNumber)
                    .ThenBy(s => s.Title)
                    .ToListAsync();
                var songsDto = songs.Adapt<List<SongDto>>();
                return ServiceResult<List<SongDto>>.Success(songsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while retrieving songs for category {CategoryId}. {Error}", categoryId, ex);
                return ServiceResult<List<SongDto>>.Failure(
                    new ServerErrorException("An error occurred while retrieving songs for the specified category."));
            }
        }
        #endregion
    }
}