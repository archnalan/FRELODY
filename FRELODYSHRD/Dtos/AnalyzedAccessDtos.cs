using FRELODYSHRD.Constants;

namespace FRELODYSHRD.Dtos
{
    /// <summary>
    /// Request to unlock (or re-confirm access to) an analysis-flow song for the
    /// current user. Title/Thumbnail are denormalized snapshots so the
    /// "Today's songs" page can render without a cross-platform join.
    /// </summary>
    public class AnalyzedAccessRequest
    {
        public AnalyzedPlatform Platform { get; set; }
        public string VideoId { get; set; } = default!;
        public string? Title { get; set; }
        public string? ThumbnailUrl { get; set; }
    }

    /// <summary>
    /// Outcome of a metered analyzed-play evaluation. <see cref="Allowed"/> is the
    /// single source of truth the caller acts on; the remaining fields drive the
    /// counter UI and the in-context paywall sheet.
    /// </summary>
    public class AnalyzedAccessResultDto
    {
        /// <summary>Whether the user may play this analyzed song right now.</summary>
        public bool Allowed { get; set; }

        /// <summary>True when this song was already unlocked within the availability window (free re-play).</summary>
        public bool AlreadyUnlocked { get; set; }

        /// <summary>True when the user has premium billing and bypasses the quota.</summary>
        public bool IsPremium { get; set; }

        /// <summary>True when a non-premium user has hit their daily limit (drives the paywall).</summary>
        public bool LimitReached { get; set; }

        /// <summary>Configured free analyzed songs per UTC day.</summary>
        public int DailyLimit { get; set; }

        /// <summary>Distinct analyzed songs the user has unlocked so far today (UTC).</summary>
        public int UsedToday { get; set; }

        /// <summary>Remaining free analyzed songs today (0 when premium-unlimited or over limit).</summary>
        public int Remaining { get; set; }

        /// <summary>Machine-readable reason when not allowed: "unauthenticated" | "limit-reached" | "too-long".</summary>
        public string? Reason { get; set; }

        /// <summary>Max analyzable source duration (seconds) for free users; 0 = unlimited.</summary>
        public int FreeMaxDurationSeconds { get; set; }

        /// <summary>Max analyzable source duration (seconds) for premium users; 0 = unlimited.</summary>
        public int PremiumMaxDurationSeconds { get; set; }
    }

    /// <summary>
    /// Public (no-auth) monetization limits, so the client can gate over-long
    /// content before prompting sign-in or running analysis.
    /// </summary>
    public class AnalyzedLimitsDto
    {
        public int FreeAnalyzedSongsPerDay { get; set; }
        public int FreeMaxDurationSeconds { get; set; }
        public int PremiumMaxDurationSeconds { get; set; }
    }

    /// <summary>
    /// An analyzed song unlocked by the user and still within its availability
    /// window — surfaced on the "Today's songs" page.
    /// </summary>
    public class AnalyzedSongDto
    {
        public AnalyzedPlatform Platform { get; set; }
        public string VideoId { get; set; } = default!;
        public string? Title { get; set; }
        public string? ThumbnailUrl { get; set; }
        /// <summary>Original source URL (TikTok replay needs it; null for YouTube).</summary>
        public string? SourceUrl { get; set; }
        public DateTimeOffset UnlockedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
