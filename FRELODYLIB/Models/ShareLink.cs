using FRELODYAPP.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYLIB.Models
{
    public class ShareLink
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        public string? SongId { get; set; } = string.Empty;

        public string? PlaylistId { get; set; }

        [Required]
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string ShareToken { get; set; } = string.Empty;

        public DateTime? CreatedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public bool? IsActive { get; set; } = true;

        // ─── Open Graph snapshot ──────────────────────────────────────────────
        // Populated at share-link creation time so crawlers (WhatsApp, iMessage,
        // Facebook, Twitter, LinkedIn, Slack, Discord, …) can render a rich
        // preview without executing any JavaScript or calling authenticated
        // endpoints. Fields represent an immutable snapshot of the shared item.
        [StringLength(200)]
        public string? OgTitle { get; set; }

        [StringLength(500)]
        public string? OgDescription { get; set; }

        /// <summary>Relative path (under wwwroot) of the generated 1200×630 PNG preview.</summary>
        [StringLength(300)]
        public string? OgImagePath { get; set; }

        /// <summary>Pre-rendered hero HTML fragment used as the visible body of the landing page.</summary>
        public string? OgHtml { get; set; }

        [ForeignKey(nameof(SongId))]
        public virtual Song? Song { get; set; }

        [ForeignKey(nameof(PlaylistId))]
        public virtual Playlist? Playlist { get; set; }
    }
}
