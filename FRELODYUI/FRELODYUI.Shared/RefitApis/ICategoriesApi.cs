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

        [Get("/api/categories/get-categories-by-song-book-id")]
        Task<IApiResponse<List<CategoryDto>>> GetCategoriesBySongBookId([Query] string songBookId);

        [Get("/api/categories/get-all-songs-by-category-id")]
        Task<IApiResponse<List<SongDto>>> GetAllSongsByCategoryId([Query] string categoryId);
    }
}