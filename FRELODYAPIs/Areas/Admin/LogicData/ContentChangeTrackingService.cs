using FRELODYAPP.Data.Infrastructure;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.SubDtos;
using FRELODYSHRD.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class ContentChangeTrackingService
    {
        private readonly SongDbContext _context;
        private readonly ITenantProvider _tenantProvider;
        private readonly ILogger<ContentChangeTrackingService> _logger;

        public ContentChangeTrackingService(ILogger<ContentChangeTrackingService> logger, ITenantProvider tenantProvider, SongDbContext context)
        {
            _logger = logger;
            _tenantProvider = tenantProvider;
            _context = context;
        }

        public async Task<ServiceResult<string>> LogContentChange(ChangeLogDto dto)
        {
            try
            {
                var changeLog = new ContentChangeLog
                {
                    EntityType = dto.EntityType,
                    EntityId = dto.EntityId,
                    ChangeType = dto.ChangeType,
                    ChangedByUserId = dto.ChangedByUserId ?? _tenantProvider.GetUserId(),
                    ChangeTime = DateTimeOffset.UtcNow,
                    ChangeDetails = dto.ChangeDetails != null ? JsonConvert.SerializeObject(dto.ChangeDetails) : null,
                    IsPublicContent = await IsPublicContent(dto.EntityType, dto.EntityId)
                };
                await _context.ContentChangeLogs.AddAsync(changeLog);
                await _context.SaveChangesAsync();

                return ServiceResult<string>.Success(changeLog.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging content change for {EntityType} {EntityId}", dto.EntityType.ToString(), dto.EntityId);
                return ServiceResult<string>.Failure(new ServerErrorException("Failed to log content change"));
            }
        }

        public async Task<ServiceResult<DashboardActivitySummary>> GetActivitySinceLastLogin(string userId)
        {
            try
            {
                var lastLogin = await _context.UserLoginHistories
                    .Where(x => x.UserId == userId && !x.IsActiveSession)
                    .OrderByDescending(x => x.LoginTime)
                    .Select(x => x.LoginTime)
                    .FirstOrDefaultAsync();

                if (lastLogin == default)
                {
                    // If no previous login, show activity from last 7 days
                    lastLogin = DateTimeOffset.UtcNow.AddDays(-7);
                }

                var newSongsCount = await _context.ContentChangeLogs
                    .Where(x => x.EntityType == EntityLogType.Song &&
                               x.ChangeType == ChangeLogType.Created &&
                               x.ChangeTime > lastLogin &&
                               x.IsPublicContent)
                    .CountAsync();

                var updatedSongsCount = await _context.ContentChangeLogs
                    .Where(x => x.EntityType == EntityLogType.Song &&
                               x.ChangeType == ChangeLogType.Updated &&
                               x.ChangeTime > lastLogin &&
                               x.IsPublicContent)
                    .CountAsync();

                var newPlaylistsCount = await _context.ContentChangeLogs
                    .Where(x => x.EntityType == EntityLogType.Playlist &&
                               x.ChangeType == ChangeLogType.Created &&
                               x.ChangeTime > lastLogin &&
                               x.IsPublicContent)
                    .CountAsync();

                // Get trending/public songs created since last login
                var newPublicSongs = await _context.ContentChangeLogs
                    .Where(x => x.EntityType == EntityLogType.Song &&
                               x.ChangeType == ChangeLogType.Created &&
                               x.ChangeTime > lastLogin &&
                               x.IsPublicContent)
                    .Join(_context.Songs,
                          log => log.EntityId,
                          song => song.Id,
                          (log, song) => new { song.Id, song.Title, song.Rating, ChangeTime = log.ChangeTime })
                    .OrderByDescending(x => x.Rating)
                    .ThenByDescending(x => x.ChangeTime)
                    .Take(5)
                    .ToListAsync();

                return ServiceResult<DashboardActivitySummary>.Success(new DashboardActivitySummary
                {
                    NewSongsCount = newSongsCount,
                    UpdatedSongsCount = updatedSongsCount,
                    NewPlaylistsCount = newPlaylistsCount,
                    LastLoginTime = lastLogin,
                    NewPublicSongs = newPublicSongs.Select(x => new SummarySongDto
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Rating = x.Rating
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting activity since last login for user {UserId}", userId);
                return ServiceResult<DashboardActivitySummary>.Failure(new ServerErrorException("Failed to get activity summary"));
            }
        }

        private async Task<bool> IsPublicContent(EntityLogType entityType, string entityId)
        {
            if (entityType == EntityLogType.Song) 
            {
                var song = await _context.Songs.FindAsync(entityId);
                return song?.Access == Access.Public;
            }
            else if (entityType == EntityLogType.Playlist) 
            {
                var playlist = await _context.Playlists.FindAsync(entityId);
                return playlist?.IsPublic == true;
            }
            else if (entityType == EntityLogType.SongBook)
            {
                var songBook = await _context.SongBooks.FindAsync(entityId);
                return songBook?.Access == Access.Public;
            }
            else if (entityType == EntityLogType.Album)
            {
                var album = await _context.Albums.FindAsync(entityId);
                return album?.Access == Access.Public;
            }
            else if (entityType == EntityLogType.Artist)
            {
                var artist = await _context.Artists.FindAsync(entityId);
                return artist?.Access == Access.Public;
            }
            return false;
        }
    }
}
