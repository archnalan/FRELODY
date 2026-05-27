namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class TikTokAnalyzeRequest
    {
        public string Url { get; set; } = default!;
        public string BeatModel { get; set; } = "beat-transformer";
        public string ChordModel { get; set; } = "chord-cnn-lstm";
        public string ChordDict { get; set; } = "full";
        public bool ForceRefresh { get; set; } = false;
    }
}
