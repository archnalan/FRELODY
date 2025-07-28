using DocumentFormat.OpenXml.Wordprocessing;
using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.ModelTypes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
	public class SongPart:BaseEntity
	{
        public string SongId { get; set; }

		[Range(0, 24)]		
		public int PartNumber { get; set; }

		public SongSection? PartName { get; set; }

		[MaxLength(100)]
        public string? PartTitle { get; set; }

		public int? RepeatCount { get; set; }

        public virtual ICollection<LyricLine>? LyricLines { get; set; }
	}

}
