using FRELODYAPP.Dtos;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.HybridDtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface ISongPlayHistoryApi
    {
        [Post("/api/song-play-history/log-song-play")]
        Task<IApiResponse<bool>> LogSongPlay([Query] string songId, [Query] string? playSource = null);

        [Get("/api/song-play-history/get-user-play-history")]
        Task<IApiResponse<List<SongPlayHistoryDto>>> GetUserSongPlayHistory([Query] string? userId = null, [Query] int offset = 0, [Query] int limit = 10);

        [Get("/api/song-play-history/get-song-play-history")]
        Task<IApiResponse<List<SongPlayHistoryDto>>> GetSongPlayHistory([Query] string songId, [Query] int offset = 0, [Query] int limit = 10);

        [Get("/api/song-play-history/get-most-played-songs")]
        Task<IApiResponse<Dictionary<string, int>>> GetMostPlayedSongs([Query] string? userId = null, [Query] int limit = 10);

        [Get("/api/song-play-history/get-play-statistics")]
        Task<IApiResponse<SongPlayStatisticsDto>> GetPlayStatistics([Query] string? userId = null, [Query] DateTime? fromDate = null, [Query] DateTime? toDate = null);

        [Get("/api/song-play-history/get-recent-plays")]
        Task<IApiResponse<List<SongPlayHistoryDto>>> GetRecentPlays([Query] string? userId = null, [Query] int limit = 5);

        [Delete("/api/song-play-history/clear-user-history")]
        Task<IApiResponse<bool>> ClearUserHistory([Query] string? userId = null);
    }
}