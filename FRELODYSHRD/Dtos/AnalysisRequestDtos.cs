using System;
using System.Collections.Generic;
using FRELODYSHRD.Constants;

namespace FRELODYSHRD.Dtos
{
    /// <summary>
    /// Client → server report that an analysis request was blocked at the duration
    /// pre-gate. The server records it for the superadmin review and answers back
    /// whether the video has been whitelisted (so the client can proceed anyway).
    /// </summary>
    public class BlockedRequestReportDto
    {
        public AnalyzedPlatform Platform { get; set; }
        public string VideoId { get; set; } = default!;
        /// <summary>One of <see cref="GateDenialReason"/>.</summary>
        public string Reason { get; set; } = default!;
        public string? Title { get; set; }
        public string? ChannelTitle { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? SourceUrl { get; set; }
        public int? DurationSeconds { get; set; }
    }

    /// <summary>Outcome of a blocked-request report.</summary>
    public class BlockedRequestReportResultDto
    {
        /// <summary>True when a superadmin has whitelisted this video — the client may analyze it despite its length.</summary>
        public bool Whitelisted { get; set; }
    }

    /// <summary>
    /// One video in the superadmin requests list: demand totals plus a per-reason
    /// breakdown (distinct users blocked for each cause) so the right action is obvious.
    /// </summary>
    public class AnalysisRequestVideoDto
    {
        public AnalyzedPlatform Platform { get; set; }
        public string VideoId { get; set; } = default!;
        public string? Title { get; set; }
        public string? ChannelTitle { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? SourceUrl { get; set; }
        public int? DurationSeconds { get; set; }

        /// <summary>Total blocked attempts across all users and days.</summary>
        public int TotalRequests { get; set; }

        /// <summary>Distinct identities blocked (each signed-in user once; anonymous counts as one).</summary>
        public int DistinctUsers { get; set; }

        public DateTimeOffset FirstRequestedAt { get; set; }
        public DateTimeOffset LastRequestedAt { get; set; }

        /// <summary>Distinct users blocked, keyed by <see cref="GateDenialReason"/>.</summary>
        public Dictionary<string, int> UsersByReason { get; set; } = new();

        /// <summary>The reason most users hit — drives the suggested action.</summary>
        public string? DominantReason { get; set; }

        /// <summary>True when at least one block was a duration cap (a whitelist candidate).</summary>
        public bool IsTooLong { get; set; }

        /// <summary>True when this video is already whitelisted.</summary>
        public bool IsWhitelisted { get; set; }
    }

    /// <summary>A video a superadmin has approved to bypass the duration caps.</summary>
    public class WhitelistedVideoDto
    {
        public AnalyzedPlatform Platform { get; set; }
        public string VideoId { get; set; } = default!;
        public string? Title { get; set; }
        public int? DurationSeconds { get; set; }
        public string? Note { get; set; }
        public string? ApprovedByEmail { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
    }

    /// <summary>Superadmin request to whitelist (or update) an over-long video.</summary>
    public class WhitelistVideoRequestDto
    {
        public AnalyzedPlatform Platform { get; set; }
        public string VideoId { get; set; } = default!;
        public string? Title { get; set; }
        public int? DurationSeconds { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// Daily success-vs-denied series for the admin outcome chart: songs actually
    /// analyzed (cached transcriptions) against requests turned away at the gate.
    /// Keys are ISO dates ("yyyy-MM-dd") over the trailing window.
    /// </summary>
    public class AnalysisOutcomeStatsDto
    {
        /// <summary>Songs successfully analyzed per UTC day (distinct transcriptions created).</summary>
        public Dictionary<string, int> AnalyzedByDate { get; set; } = new();

        /// <summary>Blocked/denied analysis requests per UTC day.</summary>
        public Dictionary<string, int> DeniedByDate { get; set; } = new();

        public int TotalAnalyzed { get; set; }
        public int TotalDenied { get; set; }
    }
}
