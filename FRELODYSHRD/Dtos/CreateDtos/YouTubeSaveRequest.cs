namespace FRELODYSHRD.Dtos.CreateDtos
{
    /// <summary>
    /// Request to persist a previously-analyzed YouTube transcription as a chord chart
    /// in the user's library. The (VideoId, BeatModel, ChordModel, ChordDict) tuple must
    /// match a cached <c>YouTubeTranscription</c> row produced by the analyze step.
    /// </summary>
    public class YouTubeSaveRequest
    {
        public string VideoId { get; set; } = default!;
        public string BeatModel { get; set; } = "beat-transformer";
        public string ChordModel { get; set; } = "chord-cnn-lstm";
        public string ChordDict { get; set; } = "full";

        /// <summary>Optional title override; falls back to the cached video title.</summary>
        public string? Title { get; set; }
    }
}
