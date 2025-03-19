using SongsWithChords.Dtos.SubDtos;
using SongsWithChords.Models;
using SongsWithChords.Models.SubModels;
using System.ComponentModel.DataAnnotations.Schema;

namespace SongsWithChords.Dtos
{
    public class LyricLineDto : BaseEntityDto
    {
        public Guid? Id { get; set; }
        public long LyricLineOrder { get; set; }
        public Guid? VerseId { get; set; }
        public Guid? ChorusId { get; set; }
        public Guid? BridgeId { get; set; }
        public ICollection<LyricSegment>? LyricSegments { get; set; }
    }
}
