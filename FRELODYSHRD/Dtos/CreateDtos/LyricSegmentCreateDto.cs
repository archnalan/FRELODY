namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class LyricSegmentCreateDto
    {
        public string Lyric { get; set; }
        public int LyricOrder { get; set; }
        public int LineNumber { get; set; }
        public int? UISegNo { get; set; }
        public string? LyricLineId { get; set; }
        public string? ChordId { get; set; }
    }
}
