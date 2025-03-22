namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class LyricSegmentCreateDto
    {
        public string Lyric { get; set; }
        public long LyricOrder { get; set; }
        public int LineNumber { get; set; }
        public long? UISegNo { get; set; }
        public Guid? LyricLineId { get; set; }
        public long? ChordId { get; set; }
    }
}
