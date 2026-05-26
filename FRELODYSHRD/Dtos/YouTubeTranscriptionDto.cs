namespace FRELODYSHRD.Dtos
{
    public class YouTubeTranscriptionDto
    {
        public int Id { get; set; }
        public string VideoId { get; set; } = default!;
        public string BeatModel { get; set; } = default!;
        public string ChordModel { get; set; } = default!;
        public string ChordDict { get; set; } = default!;
        public List<float> Beats { get; set; } = [];
        public List<ChordEventDto> Chords { get; set; } = [];
        public List<SyncedChordDto> SyncedChords { get; set; } = [];
        public float? Bpm { get; set; }
        public string? TimeSignature { get; set; }
        public string? KeySignature { get; set; }
        public float TotalProcessingSeconds { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
