using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using FRELODYAPP.Models.SubModels;

namespace FRELODYAPP.Models
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

        public int? RepeatCount { get; set; }

        public virtual ICollection<LyricLine>? LyricLines { get; set; }
	}
}
