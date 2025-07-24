using FRELODYAPP.Dtos;
using FRELODYAPP.ServiceHandler;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ICategoryService
    {
        Task<ServiceResult<List<CategoryDto>>> GetAllCategories();
    }
}