using FRELODYAPP.Dtos.SubDtos;
using FRELODYSHRD.ModelTypes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Dtos
{
    public class SongPartDto: BaseEntityDto
    {
        public string SongId { get; set; }

        public SongSection? PartName { get; set; }

        [Range(0, 24)]
        public int PartNumber { get; set; }

        [MaxLength(100)]
        public string? PartTitle { get; set; }

        public int? RepeatCount { get; set; }

        public virtual ICollection<LyricLineDto>? LyricLines { get; set; }
    }
}
