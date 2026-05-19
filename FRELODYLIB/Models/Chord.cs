using FRELODYAPP.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using FRELODYAPP.Models.SubModels;

namespace FRELODYAPP.Models
{
	public class Chord:BaseEntity
	{

		[StringLength(15)]
		// Industry-standard approach: only sanity-check that the name starts with a
		// root note letter. Real chord notation is too varied across genres for a
		// regex to be both inclusive and strict. Meaningful validation happens at the
		// use site (lookup against the standard catalog, transposition, rendering).
		[RegularExpression(@"^[A-G].*$",
		    ErrorMessage = "Chord name must start with a note letter A-G.")]
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
