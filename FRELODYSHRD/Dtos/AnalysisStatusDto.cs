namespace FRELODYSHRD.Dtos
{
    /// <summary>
    /// Lifecycle stage of a (decoupled, background) analysis job. Drives the real
    /// staged-progress UI and lets the client poll a short status endpoint instead of
    /// holding one multi-minute socket open (which a proxy/Cloudflare can cut at ~100s).
    /// </summary>
    public enum AnalysisStage
    {
        NotStarted = 0,
        Extracting = 1,
        DetectingBeats = 2,
        RecognizingChords = 3,
        Finalizing = 4,
        Done = 5,
        Failed = 6
    }

    /// <summary>
    /// Returned by analyze (submit) and analysis-status (poll). On <see cref="Stage"/> ==
    /// Done, <see cref="Result"/> carries the transcription; on Failed, <see cref="Error"/>
    /// carries the friendly message.
    /// </summary>
    public class AnalysisStatusDto
    {
        public AnalysisStage Stage { get; set; }
        public string VideoId { get; set; } = default!;
        public string? Error { get; set; }
        public YouTubeTranscriptionDto? Result { get; set; }
    }
}
