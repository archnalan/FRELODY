using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SongsWithChords.Dtos.SubDtos;
using Microsoft.AspNetCore.Http;
using SongsWithChords.Data;
using FRELODYSHRD.Dtos;

namespace SongsWithChords.Dtos
{
    public class LyricSegmentDto : BaseEntityDto
    {
        public Guid? Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Lyric { get; set; }

        public long LyricOrder { get; set; }

        public int LineNumber { get; set; }

        [NotMapped]
        [TextFileValidation(".txt", ".pdf")]
        public IFormFile? LyricUpload { get; set; }

        [StringLength(255)]
        public string? LyricFilePath { get; set; }

        public long? ChordId { get; set; }

        public Guid? LyricLineId { get; set; }

        [ForeignKey(nameof(ChordId))]
        public virtual ChordDto? Chord { get; set; }
    }
}
