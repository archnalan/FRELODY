namespace FRELODYSHRD.Dtos
{
    public class ChordEventDto
    {
        public float Time { get; set; }
        public string Chord { get; set; } = default!;
        public float Confidence { get; set; }
    }
}
