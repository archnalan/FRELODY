using SongsWithChords.Models.SubModels;
using System.ComponentModel.DataAnnotations.Schema;

namespace SongsWithChords.Models
{
	public class LyricLine:BaseEntity
	{
        public Guid Id { get; set; }
        public long LyricLineOrder { get; set; }
		public int? PartNumber { get; set; }// verse or bridge number: chorus number can be null

        //Navigation prop for verse,chorus and bridge and chord
        public Guid? VerseId { get; set; }
		public Guid? ChorusId { get; set; }
		public Guid? BridgeId { get; set; }
		public ICollection<LyricSegment>? LyricSegments { get; set; }
	}
}
