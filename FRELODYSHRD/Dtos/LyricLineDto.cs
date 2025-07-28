using FRELODYAPP.Dtos.SubDtos;
using FRELODYSHRD.ModelTypes;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Dtos
{
    public class LyricLineDto : BaseEntityDto
    {
        public int LyricLineOrder { get; set; }
        public int? PartNumber { get; set; }
        public int? RepeatCount { get; set; }
        public string? PartId { get; set; }
        public ICollection<LyricSegmentDto>? LyricSegments { get; set; }
    }
}
