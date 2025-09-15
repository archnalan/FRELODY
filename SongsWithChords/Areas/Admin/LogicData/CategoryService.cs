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

        #region Create a category
        public async Task<ServiceResult<CategoryDto>> CreateCategory(CategoryDto categoryDto)
        {
            try
            {
                if (categoryDto == null)
                {
                    return ServiceResult<CategoryDto>.Failure(
                        new BadRequestException("Category data is required"));
                }
                if (!string.IsNullOrEmpty(categoryDto.ParentCategoryId))
                {
                    bool parentExists = await _context.Categories
                        .AnyAsync(c => c.Id == categoryDto.ParentCategoryId);
                    if (!parentExists)
                    {
                        return ServiceResult<CategoryDto>.Failure(
                            new BadRequestException($"Parent category of does not exist. ID:{categoryDto.ParentCategoryId} "));
                    }
                }

                if(!string.IsNullOrEmpty(categoryDto.SongBookId))
                {
                    bool songBookExists = await _context.SongBooks
                        .AnyAsync(sb => sb.Id == categoryDto.SongBookId);
                    if (!songBookExists)
                    {
                        return ServiceResult<CategoryDto>.Failure(
                            new BadRequestException($"Song book does not exist. ID:{categoryDto.SongBookId} "));
                    }
                }

                var category = categoryDto.Adapt<Category>();
                category.Id = Guid.NewGuid().ToString();
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                var createdCategoryDto = category.Adapt<CategoryDto>();
                return ServiceResult<CategoryDto>.Success(createdCategoryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while creating a category. {Error}", ex);
                return ServiceResult<CategoryDto>.Failure(
                    new ServerErrorException("An error occurred while creating the category."));
            }
        }
        #endregion

        #region Update a category
        public async Task<ServiceResult<CategoryDto>> UpdateCategory(string categoryId, CategoryDto categoryDto)
        {
            try
            {
                if (string.IsNullOrEmpty(categoryId) || categoryDto == null)
                {
                    return ServiceResult<CategoryDto>.Failure(
                        new BadRequestException("Category ID and data are required"));
                }
                var existingCategory = await _context.Categories.FindAsync(categoryId);
                if (existingCategory == null)
                {
                    return ServiceResult<CategoryDto>.Failure(
                        new NotFoundException($"Category not found. ID: {categoryId}"));
                }
                if (!string.IsNullOrEmpty(categoryDto.ParentCategoryId))
                {
                    bool parentExists = await _context.Categories
                        .AnyAsync(c => c.Id == categoryDto.ParentCategoryId && c.Id != categoryId);
                    if (!parentExists)
                    {
                        return ServiceResult<CategoryDto>.Failure(
                            new BadRequestException($"Parent category does not exist. ID:{categoryDto.ParentCategoryId} "));
                    }
                }
                if (!string.IsNullOrEmpty(categoryDto.SongBookId))
                {
                    bool songBookExists = await _context.SongBooks
                        .AnyAsync(sb => sb.Id == categoryDto.SongBookId);
                    if (!songBookExists)
                    {
                        return ServiceResult<CategoryDto>.Failure(
                            new BadRequestException($"Song book does not exist. ID:{categoryDto.SongBookId} "));
                    }
                }
                existingCategory.Name = categoryDto.Name;
                existingCategory.ParentCategoryId = categoryDto.ParentCategoryId;
                existingCategory.Sorting = categoryDto.Sorting;
                existingCategory.CategorySlug = categoryDto.CategorySlug;
                existingCategory.SongBookId = categoryDto.SongBookId;
                _context.Categories.Update(existingCategory);
                await _context.SaveChangesAsync();
                var updatedCategoryDto = existingCategory.Adapt<CategoryDto>();
                return ServiceResult<CategoryDto>.Success(updatedCategoryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while updating category {CategoryId}. {Error}", categoryId, ex);
                return ServiceResult<CategoryDto>.Failure(
                    new ServerErrorException("An error occurred while updating the category."));
            }
        }
        #endregion

    }
}