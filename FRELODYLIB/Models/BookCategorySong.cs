using FRELODYAPP.Models.SubModels;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
    [Index(nameof(SongBookId), nameof(SongId), nameof(CategoryId), nameof(TenantId), IsUnique = true)]
    public class BookCategorySong : BaseEntity
    {
        [Required]
        public string SongBookId { get; set; } = default!;

        [Required]
        public string SongId { get; set; } = default!;
        public string? CategoryId { get; set; }

        [Range(1, 9999)]
        public int? SongNumber { get; set; } 

        [ForeignKey(nameof(SongBookId))]
        public virtual SongBook? SongBook { get; set; }

        [ForeignKey(nameof(SongId))]
        public virtual Song? Song { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual Category? Category { get; set; }
    }
}