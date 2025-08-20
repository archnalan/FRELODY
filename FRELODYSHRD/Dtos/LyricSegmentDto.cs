using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using FRELODYAPP.Dtos.SubDtos;
using Microsoft.AspNetCore.Http;
using FRELODYAPP.Data;
using FRELODYSHRD.Dtos;

namespace FRELODYAPP.Dtos
{
    public class LyricSegmentDto : BaseEntityDto
    {
        [Required]
        [StringLength(200)]
        public string Lyric { get; set; }

        public int LyricOrder { get; set; }

        public int LineNumber { get; set; }

        [NotMapped]
        [TextFileValidation(".txt", ".pdf")]
        public IFormFile? LyricUpload { get; set; }

        [StringLength(255)]
        public string? LyricFilePath { get; set; }

        public string? ChordId { get; set; }

        public string? LyricLineId { get; set; }

        [ForeignKey(nameof(ChordId))]
        public virtual ChordDto? Chord { get; set; }
    }
}
