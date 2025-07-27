using Refit;
using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.SubDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FRELODYSHRD.Dtos.CreateDtos;

namespace FRELODYUI.Shared.RefitApis
{
    public interface ISongsApi
    {
        [Get("/api/Songs/GetSongs")]
        Task<IApiResponse<IEnumerable<ComboBoxDto>>> GetSongs();

        [Get("/api/Songs/GetSongWithChordsById/{id}")]
        Task<IApiResponse<SongDto>> GetSongWithChordsById(string id);

        [Get("/api/Songs/GetSongDetailsById/{id}")]
        Task<IApiResponse<SongDto>> GetSongDetailsById(string id);

        [Post("/api/Songs/CreateSong")]
        Task<IApiResponse<SongDto>> CreateSong([Body] SimpleSongCreateDto song);

        [Put("/api/Songs/MarkSongFavoriteStatus")]
        Task<IApiResponse<bool>> MarkSongFavoriteStatus([Query] string songId, [Query] bool favorite);
    }
}
