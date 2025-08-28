namespace FRELODYUI.Shared.Services
{
    public static class ChordTransposer
    {
        private static readonly Dictionary<string, int> _sharpChromaticScale = new()
        {
            { "C", 0}, {"C#", 1}, {"D", 2}, {"D#", 3}, {"E", 4}, {"F", 5}, {"F#", 6},
            {"G", 7}, {"G#", 8}, {"A", 9}, {"A#", 10}, {"B", 11}
        };

        private static readonly Dictionary<string, int> _flatChromaticScale = new()
        {
            { "C", 0}, {"Db", 1}, {"D", 2}, {"Eb", 3}, {"E", 4}, {"F", 5}, {"Gb", 6},
            {"G", 7}, {"Ab", 8}, {"A", 9}, {"Bb", 10}, {"B", 11}
        };

        public static Dictionary<string, int> DetermineScale(IEnumerable<string> chords)
        {
            int sharpCount = chords.Sum(chord => chord.Count(c => c == '#'));
            int flatCount = chords.Sum(chord => chord.Count(c => c == 'b'));

            if (flatCount > sharpCount) return _flatChromaticScale;
            if (sharpCount > flatCount) return _sharpChromaticScale;

            return _sharpChromaticScale;
        }

        public static string[] TransposeChords(string[] originalChords, int semitones)
        {
            var scale = DetermineScale(originalChords);
            return originalChords.Select(chord => TransposeChord(chord, semitones, scale)).ToArray();
        }

        public static string TransposeChord(string chord, int semitones, Dictionary<string, int>? scale = null)
        {
            chord = chord.Trim().Replace(" ", ""); // Remove any spaces

            var match = System.Text.RegularExpressions
                .Regex.Match(chord, @"^([A-G])(#|b)?(m|maj|min|sus|aug|dim|add)?(\d+)?(/([A-G])(#|b)?)?$");

            if (!match.Success) return chord;

            var rootNote = match.Groups[1].Value + match.Groups[2].Value;
            var chordQuality = match.Groups[3].Value + match.Groups[4].Value;
            var bassNote = (!string.IsNullOrEmpty(match.Groups[5].Value)) ?
                match.Groups[6].Value + match.Groups[7].Value : string.Empty;

            // Use provided scale or determine from the chord itself
            scale ??= DetermineScale(new[] { chord });

            // Transpose the root note
            var transposedRoot = TransposeNote(rootNote, semitones, scale);
            var transposedBass = !string.IsNullOrEmpty(bassNote) ?
                TransposeNote(bassNote, semitones, scale) : string.Empty;

            return $"{transposedRoot}{chordQuality}{(string.IsNullOrEmpty(transposedBass) ? "" : "/" + transposedBass)}";
        }

        public static string TransposeNote(string note, int semitones, Dictionary<string, int> scale)
        {
            try
            {
                if (!scale.TryGetValue(note, out var noteIndex))
                    return note;

                var newIndex = (noteIndex + semitones + 12) % 12;
                var newNote = scale.FirstOrDefault(x => x.Value == newIndex).Key;
                return newNote ?? note;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
