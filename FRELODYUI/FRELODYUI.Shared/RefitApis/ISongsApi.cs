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
        [Get("/api/songs/get-songs")]
        Task<IApiResponse<IEnumerable<ComboBoxDto>>> GetSongs();

        [Get("/api/songs/get-song-with-chords-by-id/{id}")]
        Task<IApiResponse<SongDto>> GetSongWithChordsById(string id);

        [Get("/api/songs/get-song-details-by-id/{id}")]
        Task<IApiResponse<SongDto>> GetSongDetailsById(string id);

        [Post("/api/songs/create-song")]
        Task<IApiResponse<SongDto>> CreateSong([Body] SimpleSongCreateDto song);
        
        [Put("/api/songs/update-song")]
        Task<IApiResponse<SongDto>> UpdateSong([Query]string id, [Body] SimpleSongCreateDto song);

        [Put("/api/songs/mark-song-favorite-status")]
        Task<IApiResponse<bool>> MarkSongFavoriteStatus([Query] string songId, [Query] bool favorite);
    }
}
