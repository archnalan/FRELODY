namespace SongsWithChords.Dtos
{
	public class BridgeDto
	{
		public Guid SongId { get; set; }
        public string Title { get; set; }
		public int? BridgeNumber { get; set; }
        public ICollection<LyricLineDto>? LyricLines { get; set; }
    }
}
