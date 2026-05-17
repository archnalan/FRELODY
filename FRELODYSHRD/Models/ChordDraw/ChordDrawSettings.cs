namespace FRELODYSHRD.Models.ChordDraw
{
    public class ChordDrawSettings
    {
        public string? Title { get; set; }
        public int Strings { get; set; } = 6;
        public int Frets { get; set; } = 5;
        public int Position { get; set; } = 1;
        public string[]? Tuning { get; set; }
        public ChordOrientation Orientation { get; set; } = ChordOrientation.Vertical;
        public ChordStyle Style { get; set; } = ChordStyle.Normal;
        public string? Color { get; set; }
        public string? BackgroundColor { get; set; }
        public bool? NoPosition { get; set; }
        public bool? ShowFretMarkers { get; set; }
        public bool? FixedDiagramPosition { get; set; }

        public static string[] DefaultGuitarTuning => new[] { "E", "A", "D", "G", "B", "E" };

        public static ChordDrawSettings CreateDefault() => new()
        {
            Strings = 6,
            Frets = 5,
            Position = 1,
            Tuning = DefaultGuitarTuning,
            Orientation = ChordOrientation.Vertical,
            Style = ChordStyle.Normal
        };
    }
}
