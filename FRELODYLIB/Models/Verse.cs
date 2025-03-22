using FRELODYAPP.Models.SubModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
	public class Verse:BaseEntity
	{
		[Key]
		public Guid Id { get; set; }

        public Guid SongId { get; set; }

		[Range(0, 24)]		
		public int VerseNumber { get; set; }

		[MaxLength(100)]
        public string? VerseTitle { get; set; }

		public int? RepeatCount { get; set; }

        public virtual ICollection<LyricLine>? LyricLines { get; set; }
	}

}
