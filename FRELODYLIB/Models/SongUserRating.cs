using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FRELODYAPP.Models.SubModels;
using Microsoft.EntityFrameworkCore;

namespace FRELODYAPP.Models
{
    [Index(nameof(SongId), nameof(UserId), nameof(TenantId), IsUnique = true)]
    public class SongUserRating : BaseEntity
    {
        [Required]
        public string SongId { get; set; } = default!;

        public string? UserId { get; set; }

        [Range(0, 5)]
        [Column(TypeName = "decimal(3,2)")]
        public decimal Rating { get; set; }

        public int RevisionAtRating { get; set; }

        // Number of changes made by the user for this revision
        public int ModificationCount { get; set; } = 0;

        public DateTimeOffset RatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
