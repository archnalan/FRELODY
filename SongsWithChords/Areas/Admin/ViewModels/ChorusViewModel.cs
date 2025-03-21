using FRELODYAPP.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Areas.Admin.ViewModels
{
	public class ChorusViewModel
	{
		public int SongId { get; set; }
		
		[Range(0, 12)]
		[Display(Name = "Refrain")]
		public int? ChorusId { get; set; }

		[Required]
		[StringLength(200)]
		public string[] ChorusLyric { get; set; }
		public int[] ChorusLyricOrder { get; set; }

		//Adding Chords to chorus
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
