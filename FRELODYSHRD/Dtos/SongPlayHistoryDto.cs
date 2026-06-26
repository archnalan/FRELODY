using FRELODYAPP.Dtos.SubDtos;
using FRELODYSHRD.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos
{
    public class SongPlayHistoryDto : BaseEntityDto
    {
        /// <summary>Library song id; null for a Discover (analyzed-video) play.</summary>
        public string? SongId { get; set; }
        public string UserId { get; set; } = default!;
        public DateTimeOffset PlayedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? PlaySource { get; set; }
        public string? SessionId { get; set; }

        //Details for reporting
        public string? SongTitle { get; set; }
        public int? SongNumber { get; set; }

        // Discover plays: reference + snapshot so the dashboard can link/replay.
        public AnalyzedPlatform? Platform { get; set; }
        public string? VideoId { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? SourceUrl { get; set; }
    }

    public class MostPlayedSongDto
    {
        /// <summary>Library song id; null for a Discover (analyzed-video) entry.</summary>
        public string? SongId { get; set; }
        public string Title { get; set; } = default!;
        public int PlayCount { get; set; }

        // Set for Discover entries so the dashboard can open the analyzed song.
        public AnalyzedPlatform? Platform { get; set; }
        public string? VideoId { get; set; }
        public string? SourceUrl { get; set; }
    }

    /// <summary>Body for logging a Discover (analyzed video) play.</summary>
    public class LogDiscoverPlayDto
    {
        public AnalyzedPlatform Platform { get; set; }
        public string VideoId { get; set; } = default!;
        public string? Title { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? SourceUrl { get; set; }
    }
}
