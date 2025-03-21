using FRELODYAPP.Data;
using FRELODYAPP.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Areas.Admin.ViewModels
{
	public class LyricSegmentViewModel
	{
		public int? VerseId { get; set; }
		public int? ChorusId { get; set; }
		public int? BridgeId { get; set; }
		public int? ChordId { get; set; }


		[Required]
		[StringLength(200)]
		public string Lyric { get; set; }

		public int LyricOrder { get; set; }

		[NotMapped]
		[TextFileValidation(".txt", ".pdf")]
		public IFormFile? TextUpload { get; set; }

		[StringLength(255)]
		public string? TextFilePath { get; set; }

		//For Chords
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
