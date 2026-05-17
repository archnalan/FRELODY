using System.Collections.Generic;

namespace FRELODYSHRD.Models.ChordDraw
{
    /// <summary>
    /// Serializable chord chart drawing data. Matches the shape produced by svguitar
    /// so it can be passed directly to the JS preview renderer.
    /// </summary>
    public class ChordDrawData
    {
        public int Version { get; set; } = 1;
        public ChordContent Chord { get; set; } = new();
        public ChordDrawSettings Settings { get; set; } = ChordDrawSettings.CreateDefault();

        public static ChordDrawData CreateDefault() => new()
        {
            Chord = new ChordContent(),
            Settings = ChordDrawSettings.CreateDefault()
        };
    }

    public class ChordContent
    {
        public List<ChordFinger> Fingers { get; set; } = new();
        public List<ChordBarre> Barres { get; set; } = new();
    }
}
