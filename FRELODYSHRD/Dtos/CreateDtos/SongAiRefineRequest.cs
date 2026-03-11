namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class SongAiRefineRequest
    {
        public string OriginalContent { get; set; } = string.Empty;
        public List<SegmentCreateDto> Segments { get; set; } = new();
    }
}
