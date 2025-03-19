using SongsWithChords.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace SongsWithChords.Dtos
{
	public class ChordWithChartsDto
	{
		public long? Id { get; set; }
		public string ChordName { get; set; }

		[Range(1, 3)]
		public ChordDifficulty? Difficulty { get; set; }		
		public string? ChordAudioFilePath { get; set; }		
		public List<ChordChartEditDto>? Charts { get; set; }
	}
}
