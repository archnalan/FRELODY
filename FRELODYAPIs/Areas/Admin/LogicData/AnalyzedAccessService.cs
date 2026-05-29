using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPIs.Options;
using FRELODYAPP.Data.Infrastructure;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class AnalyzedAccessService : IAnalyzedAccessService
    {
        private readonly SongDbContext _db;
        private readonly ITenantProvider _tenantProvider;
        private readonly MonetizationOptions _options;
        private readonly ILogger<AnalyzedAccessService> _logger;

        public AnalyzedAccessService(
            SongDbContext db,
            ITenantProvider tenantProvider,
            IOptions<MonetizationOptions> options,
            ILogger<AnalyzedAccessService> logger)
        {
            _db = db;
            _tenantProvider = tenantProvider;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ServiceResult<AnalyzedAccessResultDto>> EvaluateAndRecord(
            AnalyzedPlatform platform, string videoId, string? title = null,
            string? thumbnailUrl = null, string? sourceUrl = null, int? durationSeconds = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(videoId))
                    return ServiceResult<AnalyzedAccessResultDto>.Failure(
                        new BadRequestException("videoId is required."));

                var limit = _options.FreeAnalyzedSongsPerDay;
                var enforce = _options.EnforceAnalyzedQuota;
                var freeDur = _options.FreeMaxDurationSeconds;
                var premiumDur = _options.PremiumMaxDurationSeconds;
                var userId = _tenantProvider.GetUserId();

                AnalyzedAccessResultDto NewResult() => new()
                {
                    DailyLimit = limit,
                    FreeMaxDurationSeconds = freeDur,
                    PremiumMaxDurationSeconds = premiumDur
                };

                // Hard duration cap: too long for even premium → no point signing in.
                if (durationSeconds is int dur && premiumDur > 0 && dur > premiumDur)
                {
                    var tooLong = NewResult();
                    tooLong.Reason = "too-long";
                    tooLong.Allowed = !enforce;
                    return ServiceResult<AnalyzedAccessResultDto>.Success(tooLong);
                }

                // Anonymous: sign-in is required to play analyzed songs. When
                // enforcement is off we let playback through for now.
                if (string.IsNullOrEmpty(userId))
                {
                    var anon = NewResult();
                    anon.Allowed = !enforce;
                    anon.IsPremium = false;
                    anon.UsedToday = 0;
                    anon.Remaining = limit;
                    anon.Reason = enforce ? "unauthenticated" : null;
                    return ServiceResult<AnalyzedAccessResultDto>.Success(anon);
                }

                var now = DateTime.UtcNow;
                var windowStart = now.AddHours(-_options.AvailabilityWindowHours);
                var todayStart = now.Date; // UTC midnight

                var isPremium = await IsPremiumAsync(userId);

                // Free users are capped at the shorter free duration.
                if (!isPremium && durationSeconds is int d2 && freeDur > 0 && d2 > freeDur)
                {
                    var tooLong = NewResult();
                    tooLong.IsPremium = false;
                    tooLong.Reason = "too-long";
                    tooLong.Allowed = !enforce;
                    return ServiceResult<AnalyzedAccessResultDto>.Success(tooLong);
                }

                // Already unlocked within the availability window → free re-play.
                var alreadyUnlocked = await _db.AnalyzedSongUnlocks.AnyAsync(u =>
                    u.UserId == userId &&
                    u.Platform == platform &&
                    u.VideoId == videoId &&
                    u.UnlockedAt >= windowStart);

                var usedToday = await CountDistinctTodayAsync(userId, todayStart);

                var result = NewResult();
                result.IsPremium = isPremium;
                result.AlreadyUnlocked = alreadyUnlocked;

                if (alreadyUnlocked)
                {
                    result.Allowed = true;
                    result.UsedToday = usedToday;
                    result.Remaining = isPremium ? 0 : Math.Max(0, limit - usedToday);
                    return ServiceResult<AnalyzedAccessResultDto>.Success(result);
                }

                if (isPremium)
                {
                    await RecordUnlockAsync(userId, platform, videoId, title, thumbnailUrl, sourceUrl, now);
                    result.Allowed = true;
                    result.Recorded = true;
                    result.UsedToday = usedToday + 1;
                    result.Remaining = 0;
                    return ServiceResult<AnalyzedAccessResultDto>.Success(result);
                }

                // Non-premium, a new song for today.
                if (usedToday >= limit)
                {
                    result.LimitReached = true;
                    result.Reason = "limit-reached";
                    result.UsedToday = usedToday;
                    result.Remaining = 0;
                    // When enforcing, this is the paywall trigger; otherwise let it
                    // through without consuming/recording a slot.
                    result.Allowed = !enforce;
                    return ServiceResult<AnalyzedAccessResultDto>.Success(result);
                }

                await RecordUnlockAsync(userId, platform, videoId, title, thumbnailUrl, sourceUrl, now);
                result.Allowed = true;
                result.Recorded = true;
                result.UsedToday = usedToday + 1;
                result.Remaining = Math.Max(0, limit - (usedToday + 1));
                return ServiceResult<AnalyzedAccessResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating analyzed access for {Platform}/{VideoId}", platform, videoId);
                return ServiceResult<AnalyzedAccessResultDto>.Failure(ex);
            }
        }

        public async Task<ServiceResult<AnalyzedAccessResultDto>> GetQuotaStatus()
        {
            try
            {
                var limit = _options.FreeAnalyzedSongsPerDay;
                var freeDur = _options.FreeMaxDurationSeconds;
                var premiumDur = _options.PremiumMaxDurationSeconds;
                var userId = _tenantProvider.GetUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResult<AnalyzedAccessResultDto>.Success(new AnalyzedAccessResultDto
                    {
                        Allowed = false,
                        IsPremium = false,
                        DailyLimit = limit,
                        UsedToday = 0,
                        Remaining = limit,
                        Reason = "unauthenticated",
                        FreeMaxDurationSeconds = freeDur,
                        PremiumMaxDurationSeconds = premiumDur
                    });
                }

                var todayStart = DateTime.UtcNow.Date;
                var isPremium = await IsPremiumAsync(userId);
                var usedToday = await CountDistinctTodayAsync(userId, todayStart);

                return ServiceResult<AnalyzedAccessResultDto>.Success(new AnalyzedAccessResultDto
                {
                    IsPremium = isPremium,
                    DailyLimit = limit,
                    UsedToday = usedToday,
                    Remaining = isPremium ? 0 : Math.Max(0, limit - usedToday),
                    Allowed = isPremium || usedToday < limit,
                    LimitReached = !isPremium && usedToday >= limit,
                    FreeMaxDurationSeconds = freeDur,
                    PremiumMaxDurationSeconds = premiumDur
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analyzed quota status");
                return ServiceResult<AnalyzedAccessResultDto>.Failure(ex);
            }
        }

        public async Task<ServiceResult<bool>> ReleaseUnlock(AnalyzedPlatform platform, string videoId)
        {
            try
            {
                var userId = _tenantProvider.GetUserId();
                if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(videoId))
                    return ServiceResult<bool>.Success(false);

                // Only today's slot is refundable. Callers gate this on Recorded==true,
                // which implies no prior unlock existed in the window — so the rows we
                // remove here are exactly the one just consumed, never an earned re-play.
                var todayStart = DateTime.UtcNow.Date;
                var rows = await _db.AnalyzedSongUnlocks
                    .Where(u => u.UserId == userId &&
                                u.Platform == platform &&
                                u.VideoId == videoId &&
                                u.UnlockedAt >= todayStart)
                    .ToListAsync();

                if (rows.Count == 0)
                    return ServiceResult<bool>.Success(false);

                _db.AnalyzedSongUnlocks.RemoveRange(rows);
                await _db.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing analyzed unlock for {Platform}/{VideoId}", platform, videoId);
                return ServiceResult<bool>.Failure(ex);
            }
        }

        public AnalyzedLimitsDto GetLimits() => new()
        {
            FreeAnalyzedSongsPerDay = _options.FreeAnalyzedSongsPerDay,
            FreeMaxDurationSeconds = _options.FreeMaxDurationSeconds,
            PremiumMaxDurationSeconds = _options.PremiumMaxDurationSeconds
        };

        public async Task<ServiceResult<List<AnalyzedSongDto>>> GetTodaysSongs()
        {
            try
            {
                var userId = _tenantProvider.GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return ServiceResult<List<AnalyzedSongDto>>.Success(new List<AnalyzedSongDto>());

                var windowStart = DateTime.UtcNow.AddHours(-_options.AvailabilityWindowHours);

                var rows = await _db.AnalyzedSongUnlocks
                    .Where(u => u.UserId == userId && u.UnlockedAt >= windowStart)
                    .OrderByDescending(u => u.UnlockedAt)
                    .ToListAsync();

                // Collapse to the most recent unlock per distinct (Platform, VideoId).
                var songs = rows
                    .GroupBy(r => new { r.Platform, r.VideoId })
                    .Select(g => g.First())
                    .Select(u =>
                    {
                        var unlockedAt = new DateTimeOffset(
                            DateTime.SpecifyKind(u.UnlockedAt, DateTimeKind.Utc));
                        return new AnalyzedSongDto
                        {
                            Platform = u.Platform,
                            VideoId = u.VideoId,
                            Title = u.Title,
                            ThumbnailUrl = u.ThumbnailUrl,
                            SourceUrl = u.SourceUrl,
                            UnlockedAt = unlockedAt,
                            ExpiresAt = unlockedAt.AddHours(_options.AvailabilityWindowHours)
                        };
                    })
                    .ToList();

                return ServiceResult<List<AnalyzedSongDto>>.Success(songs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving today's analyzed songs");
                return ServiceResult<List<AnalyzedSongDto>>.Failure(ex);
            }
        }

        private async Task<bool> IsPremiumAsync(string userId)
        {
            // SuperAdmins always have full premium privileges, regardless of billing.
            if (_tenantProvider.IsSuperAdmin(userId))
                return true;

            // Authoritative read of billing status by primary key — ignore the
            // tenant/active query filters so a self-lookup never returns null.
            var row = await _db.Users
                .IgnoreQueryFilters()
                .Where(u => u.Id == userId)
                .Select(u => new { u.BillingStatus, u.BillingExpiresAt })
                .FirstOrDefaultAsync();

            if (row is null)
                return false;

            var isPremiumTier = row.BillingStatus is BillingStatus.PremiumTrial
                or BillingStatus.ActiveRecurring
                or BillingStatus.ActiveLifetime;

            // Lifetime (null expiry) never lapses; timed grants must not be expired.
            return isPremiumTier && (row.BillingExpiresAt is null || row.BillingExpiresAt > DateTimeOffset.UtcNow);
        }

        private Task<int> CountDistinctTodayAsync(string userId, DateTime todayStart) =>
            _db.AnalyzedSongUnlocks
                .Where(u => u.UserId == userId && u.UnlockedAt >= todayStart)
                .Select(u => new { u.Platform, u.VideoId })
                .Distinct()
                .CountAsync();

        private async Task RecordUnlockAsync(
            string userId, AnalyzedPlatform platform, string videoId,
            string? title, string? thumbnailUrl, string? sourceUrl, DateTime unlockedAt)
        {
            _db.AnalyzedSongUnlocks.Add(new AnalyzedSongUnlock
            {
                UserId = userId,
                Platform = platform,
                VideoId = videoId,
                UnlockedAt = unlockedAt,
                Title = Truncate(title, 500),
                ThumbnailUrl = Truncate(thumbnailUrl, 1000),
                SourceUrl = Truncate(sourceUrl, 1000)
            });
            await _db.SaveChangesAsync();
        }

        private static string? Truncate(string? s, int max) =>
            s is null ? null : s.Length <= max ? s : s[..max];
    }
}
