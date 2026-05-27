using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
    public class TikTokVideo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // TikTok video ids are ~19-digit numeric strings.
        [Required]
        [StringLength(32)]
        public string VideoId { get; set; } = default!;

        [Required]
        [StringLength(500)]
        public string Url { get; set; } = default!;

        [Required]
        [StringLength(500)]
        public string Title { get; set; } = default!;

        [StringLength(255)]
        public string? Uploader { get; set; }

        [StringLength(1000)]
        public string? ThumbnailUrl { get; set; }

        public int DurationSeconds { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual ICollection<TikTokTranscription> Transcriptions { get; set; } = [];
    }
}
