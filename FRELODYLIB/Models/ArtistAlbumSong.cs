using FRELODYAPP.Models.SubModels;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
    [Index(nameof(ArtistId), nameof(SongId), nameof(AlbumId), nameof(TenantId), IsUnique = true)]
    public class ArtistAlbumSong : BaseEntity
    {
        [Required]
        public string ArtistId { get; set; } = default!;

        [Required]
        public string SongId { get; set; } = default!;
        
        public string? AlbumId { get; set; }

        [StringLength(50)]
        public string? Role { get; set; } // e.g., "Primary Artist", "Featured", "Cover"

        public int? TrackNumber { get; set; } // Track number if part of an album

        [ForeignKey(nameof(ArtistId))]
        public virtual Artist? Artist { get; set; }

        [ForeignKey(nameof(SongId))]
        public virtual Song? Song { get; set; }

        [ForeignKey(nameof(AlbumId))]
        public virtual Album? Album { get; set; }
    }
}