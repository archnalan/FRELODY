using FRELODYAPP.Dtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface ISongBooksApi
    {
        [Get("/api/song-books/get-all-song-books")]
        Task<IApiResponse<List<SongBookDto>>> GetAllSongBooks();

        [Get("/api/song-books/get-song-book-by-id")]
        Task<IApiResponse<SongBookDto>> GetSongBookById([Query]string id);

        [Post("/api/song-books/create-song-book")]
        Task<IApiResponse<SongBookDto>> CreateSongBook([Body] SongBookDto songBook);

        [Delete("/api/song-books/delete-song-book")]
        Task<IApiResponse<bool>> DeleteSongBook([Query]string id);
    }
}
