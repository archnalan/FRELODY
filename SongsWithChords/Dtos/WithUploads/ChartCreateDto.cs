using SongsWithChords.Data;
using System.ComponentModel.DataAnnotations;

namespace SongsWithChords.Dtos.WithUploads
{
	public class ChartCreateDto
	{
		public string FilePath { get; set; }
		public int? ChordId { get; set; }

		[Range(1, 24)]
		public int FretPosition { get; set; }

		[StringLength(255)]
		public string? ChartAudioFilePath { get; set; }

		[FileExtensionValidation(new string[] { ".png", ".jpg", ".gif", ".tiff", ".svg" })]
		public IFormFile? ChartUpload { get; set; }

		[FileExtensionValidation(new string[] { ".mp3", ".avi", ".mp4", ".aac", ".wav" })]
		public IFormFile? ChartAudioUpload { get; set; }

		[StringLength(100)]
		public string? PositionDescription { get; set; }
	}
}
