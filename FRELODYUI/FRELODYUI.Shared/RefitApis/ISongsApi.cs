using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYLIB.ServiceHandler;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.Dtos.SubDtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface ISongsApi
    {
        [Get("/api/songs/get-songs")]
        Task<IApiResponse<PaginationDetails<ComboBoxDto>>> GetSongs([Query]int offset, [Query]int limit);

        [Get("/api/songs/search-songs")]
        Task<IApiResponse<PaginationDetails<ComboBoxDto>>> SearchSongs([Query]string keywords, [Query]int offset, [Query]int limit);

        [Get("/api/songs/get-song-with-chords-by-id")]
        Task<ApiResponse<SongDto>> GetSongWithChordsById([Query] string Id);

        [Get("/api/songs/get-song-details-by-id/{id}")]
        Task<IApiResponse<SongDto>> GetSongDetailsById([Query]string id);

        [Post("/api/songs/create-song")]
        Task<IApiResponse<SongDto>> CreateSong([Body] SimpleSongCreateDto song);
        
        [Put("/api/songs/update-song")]
        Task<IApiResponse<SongDto>> UpdateSong([Query]string id, [Body] SimpleSongCreateDto song);

        [Put("/api/songs/mark-song-favorite-status")]
        Task<IApiResponse<bool>> MarkSongFavoriteStatus([Query] string songId, [Query] bool favorite);
        
        [Put("/api/songs/rate-song")]
        Task<IApiResponse<bool>> RateSong([Query] string songId, [Query] decimal rating);

        [Get("/api/songs/can-user-rate-song")]
        Task<IApiResponse<CanRateDto>> CanUserRateSong([Query] string songId);

        [Get("/api/songs/get-favorite-songs")]
        Task<IApiResponse<PaginationDetails<ComboBoxDto>>> GetFavoriteSongs([Query] string? userId = null, [Query] int? offset = 0, [Query] int? limit = 10);

        [Get("/api/songs/is-song-favorited")]
        Task<IApiResponse<bool>> IsSongFavorited([Query] string songId, [Query] string? userId = null);
        
        [Get("/api/songs/get-song-details-by-recovery-id")]
        Task<IApiResponse<SimpleSongCreateDto>> GetSongDetailsByRecoveryId([Query] string recoveryId);
        
        [Get("/api/songs/get-recovery-songs")]
        Task<IApiResponse<PaginationDetails<SongRecoveryDto>>> GetRecoverySongs([Query] string? userId = null, [Query] int? offset = 0, [Query] int? limit = 10);
        
        [Delete("/api/songs/delete-recovery-song-item")]
        Task<IApiResponse<bool>> DeleteRecoverySongItem([Query] string recoveryId);

        [Delete("/api/songs/delete-song")]
        Task<IApiResponse<bool>> DeleteSong([Query] string songId);
    }
}
