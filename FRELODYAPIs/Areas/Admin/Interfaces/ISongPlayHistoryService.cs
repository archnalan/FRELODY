using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.HybridDtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ISongPlayHistoryService
    {
        Task<ServiceResult<bool>> LogSongPlay(string songId, string? playSource = null);
        Task<ServiceResult<List<SongPlayHistoryDto>>> GetUserSongPlayHistory(string? userId = null, int offset = 0, int limit = 10);
        Task<ServiceResult<List<SongPlayHistoryDto>>> GetSongPlayHistory(string songId, int offset = 0, int limit = 10);
        Task<ServiceResult<Dictionary<string, int>>> GetMostPlayedSongs(string? userId = null, int limit = 10);
        Task<ServiceResult<SongPlayStatisticsDto>> GetPlayStatistics(string? userId = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<ServiceResult<List<SongPlayHistoryDto>>> GetRecentPlays(string? userId = null, int limit = 5);
        Task<ServiceResult<bool>> ClearUserHistory(string? userId = null);
    }
}
