namespace SongsWithChords.Dtos.CompositeDtos
{
	public class CreateFullVerseDto
	{
		public VerseCreateDto verseCreate {  get; set; }
		public List<LineVerseCreateDto> linesCreate { get; set; }
		public List<LyricSegmentCreateDto> segmentsCreate { get; set; }
		public List<ChordCreateDto> chordsCreate { get; set; }
	}
}
