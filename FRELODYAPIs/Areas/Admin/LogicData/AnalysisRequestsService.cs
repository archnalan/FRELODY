using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    /// <summary>
    /// Persistence + read model for blocked analysis requests and the per-video
    /// whitelist. See <see cref="IAnalysisRequestsService"/>.
    /// </summary>
    public class AnalysisRequestsService : IAnalysisRequestsService
    {
        private readonly SongDbContext _db;
        private readonly ITenantProvider _tenantProvider;
        private readonly ILogger<AnalysisRequestsService> _logger;

        private const string AnonKey = "\0anon"; // sentinel for distinct-user counting

        public AnalysisRequestsService(
            SongDbContext db,
            ITenantProvider tenantProvider,
            ILogger<AnalysisRequestsService> logger)
        {
            _db = db;
            _tenantProvider = tenantProvider;
            _logger = logger;
        }

        public async Task RecordAsync(
            AnalyzedPlatform platform, string videoId, string reason,
            string? userId, string? userEmail, bool wasPremium,
            string? title, string? channelTitle, string? thumbnailUrl,
            string? sourceUrl, int? durationSeconds)
        {
            if (string.IsNullOrWhiteSpace(videoId) || string.IsNullOrWhiteSpace(reason))
                return;

            try
            {
                var now = DateTime.UtcNow;
                var today = now.Date;
                userId = string.IsNullOrWhiteSpace(userId) ? null : userId;

                var existing = await _db.AnalysisRequestLogs.FirstOrDefaultAsync(r =>
                    r.Platform == platform &&
                    r.VideoId == videoId &&
                    r.UserId == userId &&
                    r.RequestDate == today);

                if (existing is not null)
                {
                    existing.RequestCount++;
                    existing.LastRequestedAt = now;
                    existing.Reason = reason; // latest classification wins for this (user, video, day)
                    existing.WasPremium = wasPremium;
                    // Self-heal metadata captured as null on an earlier hit.
                    existing.Title ??= Truncate(title, 500);
                    existing.ChannelTitle ??= Truncate(channelTitle, 255);
                    existing.ThumbnailUrl ??= Truncate(thumbnailUrl, 1000);
                    existing.SourceUrl ??= Truncate(sourceUrl, 1000);
                    existing.DurationSeconds ??= durationSeconds;
                    await _db.SaveChangesAsync();
                    return;
                }

                _db.AnalysisRequestLogs.Add(new AnalysisRequestLog
                {
                    Platform = platform,
                    VideoId = videoId,
                    UserId = userId,
                    UserEmail = Truncate(userEmail, 255),
                    Reason = reason,
                    WasPremium = wasPremium,
                    Title = Truncate(title, 500),
                    ChannelTitle = Truncate(channelTitle, 255),
                    ThumbnailUrl = Truncate(thumbnailUrl, 1000),
                    SourceUrl = Truncate(sourceUrl, 1000),
                    DurationSeconds = durationSeconds,
                    RequestDate = today,
                    RequestCount = 1,
                    FirstRequestedAt = now,
                    LastRequestedAt = now
                });
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // A concurrent first-hit raced us to the unique key; bump the now-existing row.
                try
                {
                    _db.ChangeTracker.Clear();
                    var today = DateTime.UtcNow.Date;
                    var row = await _db.AnalysisRequestLogs.FirstOrDefaultAsync(r =>
                        r.Platform == platform && r.VideoId == videoId &&
                        r.UserId == userId && r.RequestDate == today);
                    if (row is not null)
                    {
                        row.RequestCount++;
                        row.LastRequestedAt = DateTime.UtcNow;
                        row.Reason = reason;
                        await _db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to record blocked request after race for {Platform}/{VideoId}", platform, videoId);
                }
            }
            catch (Exception ex)
            {
                // Telemetry must never break the user-facing flow it logs.
                _logger.LogWarning(ex, "Failed to record blocked analysis request for {Platform}/{VideoId}", platform, videoId);
            }
        }

        public async Task<bool> IsWhitelistedAsync(AnalyzedPlatform platform, string videoId)
        {
            if (string.IsNullOrWhiteSpace(videoId)) return false;
            return await _db.AnalyzedVideoWhitelists
                .AnyAsync(w => w.Platform == platform && w.VideoId == videoId);
        }

        public async Task<ServiceResult<List<AnalysisRequestVideoDto>>> GetRequestsAsync()
        {
            try
            {
                var rows = await _db.AnalysisRequestLogs.AsNoTracking().ToListAsync();
                var whitelisted = (await _db.AnalyzedVideoWhitelists.AsNoTracking()
                        .Select(w => new { w.Platform, w.VideoId }).ToListAsync())
                    .Select(w => Key(w.Platform, w.VideoId))
                    .ToHashSet();

                var list = rows
                    .GroupBy(r => new { r.Platform, r.VideoId })
                    .Select(g =>
                    {
                        // Prefer the most-recent row for snapshot metadata.
                        var newest = g.OrderByDescending(r => r.LastRequestedAt).First();

                        string Identity(AnalysisRequestLog r) => r.UserId ?? AnonKey;

                        var usersByReason = g
                            .GroupBy(r => r.Reason)
                            .ToDictionary(
                                rg => rg.Key,
                                rg => rg.Select(Identity).Distinct().Count());

                        var dominant = usersByReason
                            .OrderByDescending(kv => kv.Value)
                            .ThenBy(kv => kv.Key)
                            .Select(kv => kv.Key)
                            .FirstOrDefault();

                        return new AnalysisRequestVideoDto
                        {
                            Platform = g.Key.Platform,
                            VideoId = g.Key.VideoId,
                            Title = g.Select(r => r.Title).FirstOrDefault(t => !string.IsNullOrEmpty(t)),
                            ChannelTitle = g.Select(r => r.ChannelTitle).FirstOrDefault(t => !string.IsNullOrEmpty(t)),
                            ThumbnailUrl = g.Select(r => r.ThumbnailUrl).FirstOrDefault(t => !string.IsNullOrEmpty(t)),
                            SourceUrl = g.Select(r => r.SourceUrl).FirstOrDefault(t => !string.IsNullOrEmpty(t)),
                            DurationSeconds = g.Select(r => r.DurationSeconds).FirstOrDefault(d => d.HasValue),
                            TotalRequests = g.Sum(r => r.RequestCount),
                            DistinctUsers = g.Select(Identity).Distinct().Count(),
                            FirstRequestedAt = AsUtc(g.Min(r => r.FirstRequestedAt)),
                            LastRequestedAt = AsUtc(g.Max(r => r.LastRequestedAt)),
                            UsersByReason = usersByReason,
                            DominantReason = dominant,
                            IsTooLong = g.Any(r => GateDenialReason.IsTooLong(r.Reason)),
                            IsWhitelisted = whitelisted.Contains(Key(g.Key.Platform, g.Key.VideoId))
                        };
                    })
                    // Most demand first, then most recent.
                    .OrderByDescending(v => v.DistinctUsers)
                    .ThenByDescending(v => v.TotalRequests)
                    .ThenByDescending(v => v.LastRequestedAt)
                    .ToList();

                return ServiceResult<List<AnalysisRequestVideoDto>>.Success(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analysis requests");
                return ServiceResult<List<AnalysisRequestVideoDto>>.Failure(ex);
            }
        }

        public async Task<ServiceResult<List<WhitelistedVideoDto>>> GetWhitelistAsync()
        {
            try
            {
                var rows = await _db.AnalyzedVideoWhitelists.AsNoTracking()
                    .OrderByDescending(w => w.DateCreated)
                    .ToListAsync();

                var list = rows.Select(w => new WhitelistedVideoDto
                {
                    Platform = w.Platform,
                    VideoId = w.VideoId,
                    Title = w.Title,
                    DurationSeconds = w.DurationSeconds,
                    Note = w.Note,
                    ApprovedByEmail = w.ApprovedByEmail,
                    ApprovedAt = w.DateCreated
                }).ToList();

                return ServiceResult<List<WhitelistedVideoDto>>.Success(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving video whitelist");
                return ServiceResult<List<WhitelistedVideoDto>>.Failure(ex);
            }
        }

        public async Task<ServiceResult<bool>> ApproveVideoAsync(WhitelistVideoRequestDto request)
        {
            try
            {
                if (request is null || string.IsNullOrWhiteSpace(request.VideoId))
                    return ServiceResult<bool>.Failure(new BadRequestException("videoId is required."));

                var email = _tenantProvider.GetCurrentUser()?.Email;

                var existing = await _db.AnalyzedVideoWhitelists.FirstOrDefaultAsync(w =>
                    w.Platform == request.Platform && w.VideoId == request.VideoId);

                if (existing is not null)
                {
                    existing.Title = Truncate(request.Title, 500) ?? existing.Title;
                    existing.DurationSeconds = request.DurationSeconds ?? existing.DurationSeconds;
                    existing.Note = Truncate(request.Note, 500);
                    existing.ApprovedByEmail = Truncate(email, 255);
                }
                else
                {
                    _db.AnalyzedVideoWhitelists.Add(new AnalyzedVideoWhitelist
                    {
                        Platform = request.Platform,
                        VideoId = request.VideoId,
                        Title = Truncate(request.Title, 500),
                        DurationSeconds = request.DurationSeconds,
                        Note = Truncate(request.Note, 500),
                        ApprovedByEmail = Truncate(email, 255)
                    });
                }

                await _db.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error whitelisting {Platform}/{VideoId}", request?.Platform, request?.VideoId);
                return ServiceResult<bool>.Failure(ex);
            }
        }

        public async Task<ServiceResult<bool>> RemoveWhitelistAsync(AnalyzedPlatform platform, string videoId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(videoId))
                    return ServiceResult<bool>.Success(false);

                var row = await _db.AnalyzedVideoWhitelists.FirstOrDefaultAsync(w =>
                    w.Platform == platform && w.VideoId == videoId);

                if (row is null)
                    return ServiceResult<bool>.Success(false);

                _db.AnalyzedVideoWhitelists.Remove(row);
                await _db.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing whitelist for {Platform}/{VideoId}", platform, videoId);
                return ServiceResult<bool>.Failure(ex);
            }
        }

        public async Task<ServiceResult<AnalysisOutcomeStatsDto>> GetOutcomeStatsAsync(int days = 30)
        {
            try
            {
                if (days < 1) days = 1;
                if (days > 365) days = 365;

                var today = DateTime.UtcNow.Date;
                var windowStart = today.AddDays(-(days - 1));
                var windowStartOffset = new DateTimeOffset(windowStart, TimeSpan.Zero);

                // Success = distinct transcriptions created (cache-once) per UTC day.
                var ytDates = await _db.YouTubeTranscriptions.AsNoTracking()
                    .Where(t => t.CreatedAt >= windowStartOffset)
                    .Select(t => t.CreatedAt).ToListAsync();
                var ttDates = await _db.TikTokTranscriptions.AsNoTracking()
                    .Where(t => t.CreatedAt >= windowStartOffset)
                    .Select(t => t.CreatedAt).ToListAsync();

                // Denied = blocked gate requests per UTC day (sum of same-day repeats).
                var denied = await _db.AnalysisRequestLogs.AsNoTracking()
                    .Where(r => r.RequestDate >= windowStart)
                    .Select(r => new { r.RequestDate, r.RequestCount })
                    .ToListAsync();

                var analyzedByDate = new Dictionary<string, int>();
                var deniedByDate = new Dictionary<string, int>();
                for (var d = windowStart; d <= today; d = d.AddDays(1))
                {
                    var key = d.ToString("yyyy-MM-dd");
                    analyzedByDate[key] = 0;
                    deniedByDate[key] = 0;
                }

                void Bump(Dictionary<string, int> map, DateTime utcDay, int n)
                {
                    var key = utcDay.ToString("yyyy-MM-dd");
                    if (map.ContainsKey(key)) map[key] += n;
                }

                foreach (var dt in ytDates) Bump(analyzedByDate, dt.UtcDateTime.Date, 1);
                foreach (var dt in ttDates) Bump(analyzedByDate, dt.UtcDateTime.Date, 1);
                foreach (var r in denied) Bump(deniedByDate, r.RequestDate.Date, r.RequestCount);

                return ServiceResult<AnalysisOutcomeStatsDto>.Success(new AnalysisOutcomeStatsDto
                {
                    AnalyzedByDate = analyzedByDate,
                    DeniedByDate = deniedByDate,
                    TotalAnalyzed = analyzedByDate.Values.Sum(),
                    TotalDenied = deniedByDate.Values.Sum()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analysis outcome stats");
                return ServiceResult<AnalysisOutcomeStatsDto>.Failure(ex);
            }
        }

        private static string Key(AnalyzedPlatform platform, string videoId) => $"{platform}:{videoId}";

        private static DateTimeOffset AsUtc(DateTime dt) =>
            new(DateTime.SpecifyKind(dt, DateTimeKind.Utc));

        private static string? Truncate(string? s, int max) =>
            s is null ? null : s.Length <= max ? s : s[..max];
    }
}
