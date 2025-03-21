using System.ComponentModel.DataAnnotations;

namespace SongsWithChords.Dtos.WithUploads
{
	public class ChartWithUploadsDto
	{
		public int Id { get; set; }
		public string FilePath { get; set; }
		public int? ChordId { get; set; }

		[Range(1, 24)]
		public int FretPosition { get; set; }

		[StringLength(255)]
		public string? ChartAudioFilePath { get; set; }
		[StringLength(100)]
		public string? PositionDescription { get; set; }
	}
}
