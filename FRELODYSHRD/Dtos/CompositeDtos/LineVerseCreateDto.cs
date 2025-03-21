namespace FRELODYAPP.Dtos.CompositeDtos
{
	public class LineVerseCreateDto
	{
		public long LyricLineOrder { get; set; }
		public Guid? VerseId { get; set; }
		public ICollection<LyricSegmentDto>? LyricSegments { get; set; }
	}
}
