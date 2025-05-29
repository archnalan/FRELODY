using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using FRELODYAPP.Models.SubModels;

namespace FRELODYAPP.Models
{
	public class Bridge:BaseEntity
	{		
		public string SongId { get; set; }

        [Range(0, 24)]
        public int? BridgeNumber { get; set; }

		[StringLength(100)]
        public string? BridgeTitle { get; set; }

        public int? RepeatCount { get; set; }

        public virtual ICollection<LyricLine>? LyricLines { get; set; }
	}
}
