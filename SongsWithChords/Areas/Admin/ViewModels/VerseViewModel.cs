using FRELODYAPP.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Areas.Admin.ViewModels
{
	public class VerseViewModel
	{
		public int SongId { get; set; }

		
		[Range(0, 12)]
		public int? VerseNumber { get; set; }

		[Display(Name = "Verse")]
		public string DisplayNumber => $"Verse {VerseNumber}";

		[Required]
		[StringLength(200)]
		public string? VerseLyric { get; set; }
		public int? VerseLyricOrder { get; set; }

		//Adding Chords to Verse
		[StringLength(15)]
		[RegularExpression(@"^([A-G])(#|b|##|bb)?(\d+|m|maj|min|sus|aug|dim|add)?(/A-G?)?$",
			ErrorMessage = "Invalid Chord Format!")]
		public string? ChordName { get; set; }
		public ChordDifficulty Difficulty { get; set; } = ChordDifficulty.Easy;

		[NotMapped]
		public IFormFile? ChordChart { get; set; }

		[StringLength(255)]
		public string? ChordChartFilePath { get; set; }
	}
}
