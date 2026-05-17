namespace FRELODYSHRD.Models.ChordDraw
{
    /// <summary>
    /// A placed finger in svguitar's coordinate system.
    /// String index is 1-based from the highest-pitch (rightmost) string in vertical orientation.
    /// Fret 0 = open string, Fret -1 = muted/silent string, Fret >= 1 = pressed at that fret.
    /// </summary>
    public class ChordFinger
    {
        public int String { get; set; }
        public int Fret { get; set; }
        public string? Text { get; set; }
        public string? Color { get; set; }
        public string? TextColor { get; set; }
        public ChordShape? Shape { get; set; }
    }
}
