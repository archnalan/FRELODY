using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.Constants;
using System.ComponentModel.DataAnnotations;

namespace FRELODYLIB.Models
{
    /// <summary>
    /// A superadmin override that lets a specific (Platform, VideoId) be analyzed even
    /// when its duration exceeds the configured caps — so a popular long song can be
    /// unblocked without raising the global limit for everyone. Checked by the
    /// analyzed-access duration gate on both the client pre-gate and the authoritative
    /// server gate. The approving admin + timestamp ride on BaseEntity (CreatedBy/DateCreated).
    /// </summary>
    public class AnalyzedVideoWhitelist : BaseEntity
    {
        public AnalyzedPlatform Platform { get; set; }

        [Required]
        [StringLength(32)]
        public string VideoId { get; set; } = default!;

        [StringLength(500)]
        public string? Title { get; set; }

        /// <summary>Source duration (seconds) at approval time, for the admin list.</summary>
        public int? DurationSeconds { get; set; }

        /// <summary>Optional superadmin note explaining the approval.</summary>
        [StringLength(500)]
        public string? Note { get; set; }

        /// <summary>Email of the approving superadmin (snapshot for the admin list).</summary>
        [StringLength(255)]
        public string? ApprovedByEmail { get; set; }
    }
}
