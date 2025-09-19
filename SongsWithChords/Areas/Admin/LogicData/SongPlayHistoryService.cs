using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.HybridDtos;
using Microsoft.EntityFrameworkCore;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class SongPlayHistoryService: ISongPlayHistoryService
    {
        private readonly SongDbContext _context;
        private readonly ITenantProvider _tenantProvider;
        private readonly string _userId;
        private readonly ILogger<SongPlayHistoryService> _logger;

        public SongPlayHistoryService(ITenantProvider tenantProvider, SongDbContext context, ILogger<SongPlayHistoryService> logger)
        {
            _tenantProvider = tenantProvider;
            _context = context;
            _userId = _tenantProvider.GetUserId();
            _logger = logger;
        }

        #region Song Play History
        public async Task<ServiceResult<bool>> LogSongPlay(string songId, string? playSource = null)
        {
            try
            {
                if (string.IsNullOrEmpty(songId))
                {
                    return ServiceResult<bool>.Failure(
                        new BadRequestException("Song ID is required."));
                }

                // Only log for authenticated users
                if (string.IsNullOrEmpty(_userId))
                {
                    return ServiceResult<bool>.Success(true); // Silent success for anonymous users
                }

                // Verify song exists
                var songExists = await _context.Songs.AnyAsync(s => s.Id == songId);
                if (!songExists)
                {
                    return ServiceResult<bool>.Failure(
                        new NotFoundException("Song not found."));
                }

                var playHistory = new SongPlayHistory
                {
                    SongId = songId,
                    UserId = _userId,
                    PlayedAt = DateTime.UtcNow,
                    PlaySource = playSource ?? "Unknown",
                    SessionId = Guid.NewGuid().ToString() // Or get from session context
                };

                await _context.SongPlayHistories.AddAsync(playHistory);
                await _context.SaveChangesAsync();

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging song play for song {SongId}", songId);
                return ServiceResult<bool>.Failure(ex);
            }
        }

        public async Task<ServiceResult<List<SongPlayHistoryDto>>> GetUserSongPlayHistory(string? userId = null, int offset = 0, int limit = 10)
        {
            try
            {
                var targetUserId = userId ?? _userId;
                if (string.IsNullOrEmpty(targetUserId))
                {
                    return ServiceResult<List<SongPlayHistoryDto>>.Failure(
                        new BadRequestException("User ID is required."));
                }

                limit = limit <= 0 ? 10 : Math.Min(limit, 100);

                var history = await _context.SongPlayHistories
                    .Where(h => h.UserId == targetUserId)
                    .Include(h => h.Song)
                    .OrderByDescending(h => h.PlayedAt)
                    .Skip(offset)
                    .Take(limit)
                    .Select(h => new SongPlayHistoryDto
                    {
                        Id = h.Id,
                        SongId = h.SongId,
                        UserId = h.UserId,
                        PlayedAt = h.PlayedAt,
                        PlaySource = h.PlaySource,
                        SessionId = h.SessionId,
                        SongTitle = h.Song.Title,
                        SongNumber = h.Song.SongNumber
                    })
                    .ToListAsync();

                return ServiceResult<List<SongPlayHistoryDto>>.Success(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user song play history for user {UserId}", userId);
                return ServiceResult<List<SongPlayHistoryDto>>.Failure(ex);
            }
        }

        public async Task<ServiceResult<List<SongPlayHistoryDto>>> GetSongPlayHistory(string songId, int offset = 0, int limit = 10)
        {
            try
            {
                if (string.IsNullOrEmpty(songId))
                {
                    return ServiceResult<List<SongPlayHistoryDto>>.Failure(
                        new BadRequestException("Song ID is required."));
                }

                limit = limit <= 0 ? 10 : Math.Min(limit, 100);

                var history = await _context.SongPlayHistories
                    .Where(h => h.SongId == songId)
                    .Include(h => h.Song)
                    .Include(h => h.User)
                    .OrderByDescending(h => h.PlayedAt)
                    .Skip(offset)
                    .Take(limit)
                    .Select(h => new SongPlayHistoryDto
                    {
                        Id = h.Id,
                        SongId = h.SongId,
                        UserId = h.UserId,
                        PlayedAt = h.PlayedAt,
                        PlaySource = h.PlaySource,
                        SessionId = h.SessionId,
                        SongTitle = h.Song.Title,
                        SongNumber = h.Song.SongNumber
                    })
                    .ToListAsync();

                return ServiceResult<List<SongPlayHistoryDto>>.Success(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving song play history for song {SongId}", songId);
                return ServiceResult<List<SongPlayHistoryDto>>.Failure(ex);
            }
        }

        public async Task<ServiceResult<Dictionary<string, int>>> GetMostPlayedSongs(string? userId = null, int limit = 10)
        {
            try
            {
                var targetUserId = userId ?? _userId;
                if (string.IsNullOrEmpty(targetUserId))
                {
                    return ServiceResult<Dictionary<string, int>>.Failure(
                        new BadRequestException("User ID is required."));
                }

                limit = limit <= 0 ? 10 : Math.Min(limit, 50);

                var mostPlayed = await _context.SongPlayHistories
                    .Where(h => h.UserId == targetUserId)
                    .Include(h => h.Song)
                    .GroupBy(h => new { h.SongId, h.Song.Title })
                    .Select(g => new {
                        SongTitle = g.Key.Title,
                        PlayCount = g.Count()
                    })
                    .OrderByDescending(x => x.PlayCount)
                    .Take(limit)
                    .ToDictionaryAsync(x => x.SongTitle, x => x.PlayCount);

                return ServiceResult<Dictionary<string, int>>.Success(mostPlayed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving most played songs for user {UserId}", userId);
                return ServiceResult<Dictionary<string, int>>.Failure(ex);
            }
        }
        #endregion

        #region Get Play Statistics
        public async Task<ServiceResult<SongPlayStatisticsDto>> GetPlayStatistics(string? userId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var targetUserId = userId ?? _userId;
                if (string.IsNullOrEmpty(targetUserId))
                {
                    return ServiceResult<SongPlayStatisticsDto>.Failure(
                        new BadRequestException("User ID is required."));
                }

                // Set default date range if not provided
                fromDate ??= DateTime.UtcNow.AddMonths(-1); // Last month by default
                toDate ??= DateTime.UtcNow;

                var query = _context.SongPlayHistories
                    .Where(h => h.UserId == targetUserId &&
                               h.PlayedAt >= fromDate &&
                               h.PlayedAt <= toDate)
                    .Include(h => h.Song);

                var totalPlays = await query.CountAsync();

                var uniqueSongs = await query
                    .Select(h => h.SongId)
                    .Distinct()
                    .CountAsync();

                var firstPlay = await query
                    .OrderBy(h => h.PlayedAt)
                    .Select(h => h.PlayedAt)
                    .FirstOrDefaultAsync();

                var lastPlay = await query
                    .OrderByDescending(h => h.PlayedAt)
                    .Select(h => h.PlayedAt)
                    .FirstOrDefaultAsync();

                var mostPlayedSong = await query
                    .GroupBy(h => new { h.SongId, h.Song.Title })
                    .Select(g => new {
                        SongTitle = g.Key.Title,
                        PlayCount = g.Count()
                    })
                    .OrderByDescending(x => x.PlayCount)
                    .FirstOrDefaultAsync();

                var playsBySource = await query
                    .GroupBy(h => h.PlaySource ?? "Unknown")
                    .Select(g => new {
                        Source = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.Source, x => x.Count);

                var playsByDate = await query
                    .GroupBy(h => h.PlayedAt.Date)
                    .Select(g => new {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.Date, x => x.Count);

                var statistics = new SongPlayStatisticsDto
                {
                    TotalPlays = totalPlays,
                    UniqueSongs = uniqueSongs,
                    FirstPlay = firstPlay != default ? firstPlay : null,
                    LastPlay = lastPlay != default ? lastPlay : null,
                    MostPlayedSongTitle = mostPlayedSong?.SongTitle,
                    MostPlayedSongCount = mostPlayedSong?.PlayCount ?? 0,
                    PlaysBySource = playsBySource,
                    PlaysByDate = playsByDate
                };

                return ServiceResult<SongPlayStatisticsDto>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving play statistics for user {UserId}", userId);
                return ServiceResult<SongPlayStatisticsDto>.Failure(ex);
            }
        }
        #endregion

        #region Get Recent Plays
        public async Task<ServiceResult<List<SongPlayHistoryDto>>> GetRecentPlays(string? userId = null, int limit = 5)
        {
            try
            {
                var targetUserId = userId ?? _userId;
                if (string.IsNullOrEmpty(targetUserId))
                {
                    return ServiceResult<List<SongPlayHistoryDto>>.Failure(
                        new BadRequestException("User ID is required."));
                }

                limit = limit <= 0 ? 5 : Math.Min(limit, 20); // Max 20 recent plays

                var recentPlays = await _context.SongPlayHistories
                    .Where(h => h.UserId == targetUserId)
                    .Include(h => h.Song)
                    .OrderByDescending(h => h.PlayedAt)
                    .Take(limit)
                    .Select(h => new SongPlayHistoryDto
                    {
                        Id = h.Id,
                        SongId = h.SongId,
                        UserId = h.UserId,
                        PlayedAt = h.PlayedAt,
                        PlaySource = h.PlaySource,
                        SessionId = h.SessionId,
                        SongTitle = h.Song.Title,
                        SongNumber = h.Song.SongNumber
                    })
                    .ToListAsync();

                return ServiceResult<List<SongPlayHistoryDto>>.Success(recentPlays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent plays for user {UserId}", userId);
                return ServiceResult<List<SongPlayHistoryDto>>.Failure(ex);
            }
        }
        #endregion

        #region Clear User History
        public async Task<ServiceResult<bool>> ClearUserHistory(string? userId = null)
        {
            try
            {
                var targetUserId = userId ?? _userId;
                if (string.IsNullOrEmpty(targetUserId))
                {
                    return ServiceResult<bool>.Failure(
                        new BadRequestException("User ID is required."));
                }

                // For security, ensure users can only clear their own history unless they have admin rights
                if (targetUserId != _userId)
                {
                    // You might want to add role-based authorization here
                    // For now, we'll allow it but log it
                    _logger.LogWarning("User {CurrentUserId} is attempting to clear history for user {TargetUserId}",
                        _userId, targetUserId);
                }

                var historyToDelete = await _context.SongPlayHistories
                    .Where(h => h.UserId == targetUserId)
                    .ToListAsync();

                if (historyToDelete.Any())
                {
                    _context.SongPlayHistories.RemoveRange(historyToDelete);
                    var deletedCount = await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleared {Count} play history records for user {UserId}",
                        deletedCount, targetUserId);
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing user history for user {UserId}", userId);
                return ServiceResult<bool>.Failure(ex);
            }
        }
        #endregion
    }
}
