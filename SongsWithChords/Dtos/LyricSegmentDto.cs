using SongsWithChords.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SongsWithChords.Models.SubModels;
using SongsWithChords.Models;
using SongsWithChords.Dtos.SubDtos;

namespace SongsWithChords.Dtos
{
    public class LyricSegmentDto : BaseEntityDto
    {
        public Guid? Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Lyric { get; set; }

        public long LyricOrder { get; set; }

        [NotMapped]
        [TextFileValidation(".txt", ".pdf")]
        public IFormFile? LyricUpload { get; set; }

        [StringLength(255)]
        public string? LyricFilePath { get; set; }

        public long? ChordId { get; set; }

        public Guid? LyricLineId { get; set; }

        [ForeignKey(nameof(ChordId))]
        public virtual Chord? Chord { get; set; }
    }
}
