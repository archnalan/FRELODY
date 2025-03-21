namespace SongsWithChords.Dtos
{
	public class LyricLineVerseDto
	{
		public long LyricLineOrder { get; set; }
        public int? PartNumber { get; set; }// verse or bridge number: chorus number can be null
        public Guid VerseId { get; set; }
	}
}
