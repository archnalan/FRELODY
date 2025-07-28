using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.ModelTypes;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
	public class LyricLine:BaseEntity
	{
        public long LyricLineOrder { get; set; }
        public int? PartNumber { get; set; }// verse or bridge number: chorus number can be null
		public int? RepeatCount { get; set; }

        //Navigation prop for verse,chorus and bridge and chord
        public string? PartId { get; set; }
		public ICollection<LyricSegment>? LyricSegments { get; set; }
	}
}
