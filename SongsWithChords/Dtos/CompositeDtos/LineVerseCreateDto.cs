using SongsWithChords.Models;

namespace SongsWithChords.Dtos.CompositeDtos
{
	public class LineVerseCreateDto
	{
		public long LyricLineOrder { get; set; }
		public Guid? VerseId { get; set; }
		public ICollection<LyricSegment>? LyricSegments { get; set; }
	}
}
