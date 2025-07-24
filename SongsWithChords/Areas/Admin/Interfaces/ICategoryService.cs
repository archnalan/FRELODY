using FRELODYAPP.Dtos;
using FRELODYAPP.ServiceHandler;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ICategoryService
    {
        Task<ServiceResult<List<CategoryDto>>> GetAllCategories();
        Task<ServiceResult<List<SongDto>>> GetAllSongsByCategoryId(string categoryId);
        Task<ServiceResult<List<CategoryDto>>> GetCategoriesBySongBookId(string songBookId);
    }
}