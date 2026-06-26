using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.Constants;
using System.ComponentModel.DataAnnotations;

namespace FRELODYLIB.Models
{
    /// <summary>
    /// A blocked analysis request, aggregated per (Platform, VideoId, UserId, RequestDate).
    /// When a user is turned away at the access gate — video too long, daily quota
    /// reached, or sign-in required — we upsert this row: the first hit of the day
    /// inserts, repeats bump <see cref="RequestCount"/> + <see cref="LastRequestedAt"/>.
    ///
    /// Keeping the UTC day in the key keeps the table compact (a user retrying the same
    /// video all day = one row) while still giving the success-vs-denied chart a daily
    /// time series. Grouping by (Platform, VideoId) yields the demand-ranked,
    /// reason-classified list the superadmin acts on. Anonymous requests carry a null
    /// <see cref="UserId"/> (one shared row per video per day).
    /// </summary>
    public class AnalysisRequestLog : BaseEntity
    {
        public AnalyzedPlatform Platform { get; set; }

        /// <summary>YouTube ids are 11 chars; TikTok ids up to 32.</summary>
        [Required]
        [StringLength(32)]
        public string VideoId { get; set; } = default!;

        /// <summary>Null for anonymous (signed-out) requests.</summary>
        [StringLength(450)]
        public string? UserId { get; set; }

        /// <summary>Email snapshot at request time so the requests list renders without a user join.</summary>
        [StringLength(255)]
        public string? UserEmail { get; set; }

        /// <summary>Latest denial classification — see <see cref="GateDenialReason"/>.</summary>
        [Required]
        [StringLength(40)]
        public string Reason { get; set; } = default!;

        /// <summary>True when the requester held premium billing at request time
        /// (a premium user hitting "too-long-hard" is the strongest whitelist signal).</summary>
        public bool WasPremium { get; set; }

        // Denormalized video snapshots so the requests page renders without a cross-platform join.
        [StringLength(500)]
        public string? Title { get; set; }

        [StringLength(255)]
        public string? ChannelTitle { get; set; }

        [StringLength(1000)]
        public string? ThumbnailUrl { get; set; }

        /// <summary>Original source URL (needed to replay/analyze TikTok by URL); null for YouTube.</summary>
        [StringLength(1000)]
        public string? SourceUrl { get; set; }

        /// <summary>Source duration (seconds) that tripped a too-long block; null when unknown.</summary>
        public int? DurationSeconds { get; set; }

        /// <summary>UTC calendar day of the request (date component only) — part of the upsert key.</summary>
        public DateTime RequestDate { get; set; } = DateTime.UtcNow.Date;

        /// <summary>How many times this (user, video) was blocked on <see cref="RequestDate"/>.</summary>
        public int RequestCount { get; set; } = 1;

        public DateTime FirstRequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastRequestedAt { get; set; } = DateTime.UtcNow;
    }
}
