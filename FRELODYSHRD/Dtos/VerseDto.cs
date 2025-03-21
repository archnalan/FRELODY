using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SongsWithChords.Dtos.SubDtos;

namespace SongsWithChords.Dtos
{
    public class VerseDto: BaseEntityDto
    {
        public Guid? Id { get; set; }

        public Guid SongId { get; set; }

        [Range(0, 24)]
        public int VerseNumber { get; set; }

        [MaxLength(100)]
        public string? VerseTitle { get; set; }

        public virtual ICollection<LyricLineDto>? LyricLines { get; set; }
    }
}
