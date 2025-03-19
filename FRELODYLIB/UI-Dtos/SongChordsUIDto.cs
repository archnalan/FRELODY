using SongsWithChords.Dtos;

namespace SongsWithChords.UI_Dtos
{
	public class SongChordsUIDto
	{
		public string Title { get; set; }
		public long? SongNumber { get; set; }
		public List<VerseUIDto> Verses { get; set; }
	}
}
