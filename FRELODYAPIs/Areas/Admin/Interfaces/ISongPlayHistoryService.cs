using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.HybridDtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ISongPlayHistoryService
    {
        Task<ServiceResult<bool>> LogSongPlay(string songId, string? playSource = null);

        /// <summary>Logs a play of an analyzed Discover video (YouTube/TikTok) as a dated
        /// play event, so it counts toward the same dashboard charts as library plays.</summary>
        Task<ServiceResult<bool>> LogDiscoverPlay(AnalyzedPlatform platform, string videoId,
            string? title = null, string? thumbnailUrl = null, string? sourceUrl = null);
        Task<ServiceResult<List<SongPlayHistoryDto>>> GetUserSongPlayHistory(string? userId = null, int offset = 0, int limit = 10);
        Task<ServiceResult<List<SongPlayHistoryDto>>> GetSongPlayHistory(string songId, int offset = 0, int limit = 10);
        Task<ServiceResult<List<MostPlayedSongDto>>> GetMostPlayedSongs(string? userId = null, int limit = 10);
        Task<ServiceResult<SongPlayStatisticsDto>> GetPlayStatistics(string? userId = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<ServiceResult<List<SongPlayHistoryDto>>> GetRecentPlays(string? userId = null, int limit = 5);
        Task<ServiceResult<bool>> ClearUserHistory(string? userId = null);
    }
}
