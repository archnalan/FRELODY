using SongsWithChords.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace SongsWithChords.Dtos
{
	public class ChordWithOneChartDto
	{
		public string ChordName { get; set; }

		[Range(1, 3)]
		public ChordDifficulty? Difficulty { get; set; }
		public string? ChordAudioFilePath { get; set; }
		public ChordChartEditDto? ChordChart { get; set; }
	}
}
