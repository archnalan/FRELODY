using FRELODYAPP.Dtos;
using FRELODYLIB.ServiceHandler.ResultModels;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ICategoryService
    {
        Task<ServiceResult<CategoryDto>> CreateCategory(CategoryDto categoryDto);
        Task<ServiceResult<List<CategoryDto>>> GetAllCategories();
        Task<ServiceResult<List<SongDto>>> GetAllSongsByCategoryId(string categoryId);
        Task<ServiceResult<List<CategoryDto>>> GetCategoriesBySongBookId(string songBookId);
        Task<ServiceResult<CategoryDto>> GetCategoryById(string categoryId);
        Task<ServiceResult<CategoryDto>> UpdateCategory(string categoryId, CategoryDto categoryDto);
    }
}