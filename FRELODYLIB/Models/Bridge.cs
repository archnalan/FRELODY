using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SongsWithChords.Models.SubModels;

namespace SongsWithChords.Models
{
	public class Bridge:BaseEntity
	{
		[Key]
		public Guid Id { get; set; }
		
		public Guid SongId { get; set; }

        [Range(0, 24)]
        public int? BridgeNumber { get; set; }

		[StringLength(100)]
        public string? BridgeTitle { get; set; }

		[ForeignKey(nameof(SongId))]
		public virtual Song? Song { get; set; }

		public virtual ICollection<LyricLine>? LyricLines { get; set; }
	}
}
