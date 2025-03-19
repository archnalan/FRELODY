using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SongsWithChords.Models.SubModels;

namespace SongsWithChords.Models
{
	public class Chorus:BaseEntity
	{
		[Key]
		public Guid Id { get; set; }

		public Guid SongId { get; set; }

        [Range(0, 12)]
        public int? ChorusNumber { get; set; }

        public string? ChorusTitle { get; set; }

		[ForeignKey(nameof(SongId))]
		public virtual Song? Song { get; set; }

		public virtual ICollection<LyricLine>? LyricLines { get; set; }
	}
}
