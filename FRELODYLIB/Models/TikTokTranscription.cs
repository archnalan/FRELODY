using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
    public class TikTokTranscription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(32)]
        public string VideoId { get; set; } = default!;

        [Required]
        [StringLength(100)]
        public string BeatModel { get; set; } = default!;

        [Required]
        [StringLength(100)]
        public string ChordModel { get; set; } = default!;

        [Required]
        [StringLength(50)]
        public string ChordDict { get; set; } = default!;

        [Required]
        public string BeatsJson { get; set; } = default!;

        [Required]
        public string ChordsJson { get; set; } = default!;

        [Required]
        public string SyncedChordsJson { get; set; } = default!;

        public float? Bpm { get; set; }

        [StringLength(10)]
        public string? TimeSignature { get; set; }

        [StringLength(50)]
        public string? KeySignature { get; set; }

        public float TotalProcessingSeconds { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(VideoId))]
        public virtual TikTokVideo? Video { get; set; }
    }
}
