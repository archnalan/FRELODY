namespace FRELODYSHRD.Dtos
{
    public class SyncedChordDto
    {
        public float Time { get; set; }
        public string Chord { get; set; } = default!;
        public int BeatIndex { get; set; }
    }
}
