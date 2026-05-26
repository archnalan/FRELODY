using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
    public class YouTubeVideo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(11)]
        public string VideoId { get; set; } = default!;

        [Required]
        [StringLength(500)]
        public string Title { get; set; } = default!;

        [StringLength(255)]
        public string? ChannelTitle { get; set; }

        [StringLength(500)]
        public string? ThumbnailUrl { get; set; }

        public int DurationSeconds { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual ICollection<YouTubeTranscription> Transcriptions { get; set; } = [];
    }
}
