using FRELODYAPP.Dtos.SubDtos;
using FRELODYSHRD.ModelTypes;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Dtos
{
    public class LyricLineDto : BaseEntityDto
    {
        public int LyricLineOrder { get; set; }
        public SongSection PartName { get; set; }
        public int? PartNumber { get; set; }// verse or bridge number: chorus number can be null
        public int? RepeatCount { get; set; }
        public string? VerseId { get; set; }
        public string? ChorusId { get; set; }
        public string? BridgeId { get; set; }
        public ICollection<LyricSegmentDto>? LyricSegments { get; set; }
    }
}
