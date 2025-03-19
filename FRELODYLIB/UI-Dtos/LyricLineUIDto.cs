using SongsWithChords.Dtos;

namespace SongsWithChords.UI_Dtos
{
	public class LyricLineUIDto
	{
		public long LyricLineOrder { get; set; }
		public List<LyricSegmentUIDto> LyricSegments { get; set; }
	}
}
