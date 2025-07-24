using FRELODYAPP.Dtos;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface ICategoriesApi
    {
        [Get("/api/categories")]
        Task<IApiResponse<List<CategoryDto>>> GetAllCategories();
    }
}