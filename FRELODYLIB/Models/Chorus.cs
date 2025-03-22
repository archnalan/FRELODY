using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using FRELODYAPP.Models.SubModels;

namespace FRELODYAPP.Models
{
	public class Chorus:BaseEntity
	{
		[Key]
		public Guid Id { get; set; }

		public Guid SongId { get; set; }

        [Range(0, 12)]
        public int? ChorusNumber { get; set; }

        public string? ChorusTitle { get; set; }

        public int? RepeatCount { get; set; }

        public virtual ICollection<LyricLine>? LyricLines { get; set; }
	}
}
