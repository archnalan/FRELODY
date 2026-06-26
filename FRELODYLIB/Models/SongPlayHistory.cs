using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYLIB.Models
{
    /// <summary>
    /// One play event. Covers both library plays (<see cref="SongId"/> set, FK to
    /// <see cref="Song"/>) and Discover plays of analyzed YouTube/TikTok videos
    /// (<see cref="SongId"/> null; <see cref="Platform"/> + <see cref="VideoId"/> set
    /// with a denormalized title/thumbnail snapshot). Unifying them here means the
    /// dashboard play charts and most-played counts span both workflows.
    /// </summary>
    public class SongPlayHistory : BaseEntity
    {
        /// <summary>Library song id; null for a Discover (analyzed-video) play.</summary>
        public string? SongId { get; set; }

        public string? UserId { get; set; }

        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

        [StringLength(50)]// (e.g., "SongList", "Search", "Favorites", "Discover-YouTube")
        public string? PlaySource { get; set; }

        [StringLength(50)]
        public string? SessionId { get; set; }

        // ── Discover (analyzed video) plays ──────────────────────────────
        /// <summary>Source platform for a Discover play; null for a library play.</summary>
        public AnalyzedPlatform? Platform { get; set; }

        [StringLength(32)]
        public string? VideoId { get; set; }

        /// <summary>Title snapshot for a Discover play (library plays use Song.Title).</summary>
        [StringLength(500)]
        public string? MediaTitle { get; set; }

        [StringLength(1000)]
        public string? ThumbnailUrl { get; set; }

        /// <summary>Source URL (TikTok replay needs it; null for YouTube/library).</summary>
        [StringLength(1000)]
        public string? SourceUrl { get; set; }

        // Navigation properties
        public virtual Song? Song { get; set; }
        public virtual User? User { get; set; } = default!;
    }
}
