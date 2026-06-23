namespace FRELODYSHRD.Dtos
{
    /// <summary>
    /// Result of the lightweight "can this analysis be saved as a chord chart?" pre-check.
    /// A YouTube analysis is only worth saving when it can be matched with synced lyrics —
    /// a chords-only grid makes a poor library artifact, so the UI hides the save action
    /// (and never promises it) when <see cref="Saveable"/> is false.
    /// </summary>
    public class SaveabilityDto
    {
        public bool Saveable { get; set; }

        /// <summary>Optional machine-readable reason when not saveable (e.g. "no-lyrics", "no-chords").</summary>
        public string? Reason { get; set; }
    }
}
