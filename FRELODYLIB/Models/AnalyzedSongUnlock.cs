using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.Constants;
using System.ComponentModel.DataAnnotations;

namespace FRELODYLIB.Models
{
    /// <summary>
    /// Records that a user unlocked an analysis-flow song (YouTube/TikTok chord
    /// detection). One row is the unit of the daily free quota and the 24h
    /// availability window. Re-playing the same (Platform, VideoId) within the
    /// window reuses the existing row and does not consume a new slot.
    /// </summary>
    public class AnalyzedSongUnlock : BaseEntity
    {
        [Required]
        public string UserId { get; set; } = default!;

        public AnalyzedPlatform Platform { get; set; }

        /// <summary>YouTube ids are 11 chars; TikTok ids up to 32.</summary>
        [Required]
        [StringLength(32)]
        public string VideoId { get; set; } = default!;

        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

        // Denormalized snapshots so the "Today's songs" page renders without a
        // cross-platform join to YouTubeVideo / TikTokVideo.
        [StringLength(500)]
        public string? Title { get; set; }

        [StringLength(1000)]
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// Original source URL needed to replay the song from the "Today's songs"
        /// page. Set for TikTok (whose playback route takes a URL); null for
        /// YouTube, which replays by VideoId.
        /// </summary>
        [StringLength(1000)]
        public string? SourceUrl { get; set; }

        public virtual User? User { get; set; }
    }
}
