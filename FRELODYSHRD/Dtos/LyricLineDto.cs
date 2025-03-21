using FRELODYAPP.Dtos.SubDtos;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Dtos
{
    public class LyricLineDto : BaseEntityDto
    {
        public Guid? Id { get; set; }
        public long LyricLineOrder { get; set; }
        public int? PartNumber { get; set; }// verse or bridge number: chorus number can be null
        public Guid? VerseId { get; set; }
        public Guid? ChorusId { get; set; }
        public Guid? BridgeId { get; set; }
        public ICollection<LyricSegmentDto>? LyricSegments { get; set; }
    }
}
