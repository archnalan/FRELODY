using SongsWithChords.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SongsWithChords.Models.SubModels;

namespace SongsWithChords.Models
{
	public class Chord:BaseEntity
	{
		[Key]
		public long Id { get; set; }

		[StringLength(15)]
		[RegularExpression(@"^([A-G])(#|b|bb|##)?(m|maj|min|sus|aug|dim|add)?(\d+)?(/([A-G])(#|b|bb|##)?)?$",
		ErrorMessage = "Invalid Chord Format!")]
		public string ChordName { get; set; }

		[Range(1, 3)]
		public ChordDifficulty? Difficulty { get; set; }		
		public ChordType? ChordType { get; set; }

		//Will be assigned path of guitar chord position 1
		[StringLength(255)]
		public string? ChordAudioFilePath { get; set; }

		
		public virtual ICollection<ChordChart>? ChordCharts { get; set; }

		public virtual ICollection<LyricSegment>? LyricSegments { get; set; }

        public Chord()
        {
            ChordCharts = new HashSet<ChordChart>();
			LyricSegments = new HashSet<LyricSegment>();
        }
    }
	
}
