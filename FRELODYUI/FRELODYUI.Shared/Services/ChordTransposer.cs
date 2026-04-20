using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FRELODYUI.Shared.Services
{
    /// <summary>
    /// Music-theory primitives + chord/key transposition. Single source of truth
    /// used by Compose (SongBoard, KeyDropdown), Play (PlaylistDetail, Song player,
    /// PlaylistSongItem) and any future tooling that needs pitch math.
    ///
    /// Notes use the chromatic 12-tone system. Letters A–G are the natural notes;
    /// any number of '#' (sharp, +1 semitone) or 'b' (flat, -1 semitone) accidentals
    /// may follow, e.g. "C", "C#", "Db", "F##", "Abb". A trailing lowercase 'm'
    /// denotes minor quality.
    /// </summary>
    public static class ChordTransposer
    {
        // ---------- Pitch primitives ----------

        // Natural-note pitch classes (semitones above C).
        private static readonly Dictionary<char, int> _naturals = new()
        {
            { 'C', 0 }, { 'D', 2 }, { 'E', 4 }, { 'F', 5 },
            { 'G', 7 }, { 'A', 9 }, { 'B', 11 }
        };

        /// <summary>Canonical sharp spelling, indexed by pitch class 0..11.</summary>
        public static readonly string[] SharpScale =
            { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        /// <summary>Canonical flat spelling, indexed by pitch class 0..11.</summary>
        public static readonly string[] FlatScale =
            { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };

        /// <summary>Common keys offered in pickers (no double accidentals — those are still parsed correctly).</summary>
        public static readonly IReadOnlyList<string> CommonKeys = new[]
        {
            "C", "C#", "Db", "D", "D#", "Eb", "E", "F",
            "F#", "Gb", "G", "G#", "Ab", "A", "A#", "Bb", "B",
            "Cm", "C#m", "Dm", "D#m", "Ebm", "Em", "Fm",
            "F#m", "Gm", "G#m", "Abm", "Am", "A#m", "Bbm", "Bm"
        };

        // Lookup dictionaries kept for backward compatibility with
        // DetermineScale callers that pass the result back into TransposeChord.
        private static readonly Dictionary<string, int> _sharpChromaticScale = BuildScaleLookup(SharpScale);
        private static readonly Dictionary<string, int> _flatChromaticScale = BuildScaleLookup(FlatScale);

        private static Dictionary<string, int> BuildScaleLookup(string[] scale)
        {
            var dict = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < scale.Length; i++) dict[scale[i]] = i;
            return dict;
        }

        // ---------- Key-level helpers (used by KeyDropdown / SongBoard) ----------

        /// <summary>
        /// Returns the pitch class (0..11) of <paramref name="key"/>, or null if
        /// the input is null/empty/unrecognised. Supports any number of #/b
        /// accidentals (e.g. "Abb" → 7, "C##" → 2) and an optional trailing "m".
        /// </summary>
        public static int? SemitoneOf(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            var span = key.AsSpan().Trim();
            if (span.IsEmpty) return null;

            // Strip a single trailing 'm' (minor) — case-sensitive 'm', as 'M' often means major7.
            if (span[^1] == 'm') span = span[..^1];
            if (span.IsEmpty) return null;

            var letter = char.ToUpperInvariant(span[0]);
            if (!_naturals.TryGetValue(letter, out var semi)) return null;

            for (int i = 1; i < span.Length; i++)
            {
                switch (span[i])
                {
                    case '#': semi++; break;
                    case 'b': semi--; break;
                    default: return null; // unknown character → invalid key
                }
            }
            return Mod12(semi);
        }

        /// <summary>True when the key string ends with a lowercase 'm' (minor).</summary>
        public static bool IsMinor(string? key) =>
            !string.IsNullOrEmpty(key) && key[^1] == 'm';

        /// <summary>
        /// Computes the shortest signed semitone delta from <paramref name="from"/>
        /// to <paramref name="to"/>, normalised to the range [-6, 6]. Returns 0 if
        /// either side is missing or unparseable.
        /// </summary>
        public static int Delta(string? from, string? to)
        {
            var a = SemitoneOf(from);
            var b = SemitoneOf(to);
            if (a is null || b is null) return 0;

            var diff = (b.Value - a.Value) % 12;
            if (diff > 6) diff -= 12;
            if (diff < -6) diff += 12;
            return diff;
        }

        // ---------- Scale preference ----------

        /// <summary>
        /// Picks the chromatic-scale lookup that best matches the input chords'
        /// accidental spelling preference (more flats → flat scale, etc.).
        /// </summary>
        public static Dictionary<string, int> DetermineScale(IEnumerable<string> chords)
        {
            int sharpCount = chords.Sum(chord => chord.Count(c => c == '#'));
            int flatCount = chords.Sum(chord => chord.Count(c => c == 'b'));

            if (flatCount > sharpCount) return _flatChromaticScale;
            return _sharpChromaticScale;
        }

        private static bool PrefersFlat(Dictionary<string, int>? scale) =>
            ReferenceEquals(scale, _flatChromaticScale);

        // ---------- Chord transposition ----------

        // Permits multi-character accidentals (Abb, F##) on root and bass notes.
        private static readonly Regex _chordRegex = new(
            @"^([A-G])([#b]*)(m|maj|min|sus|aug|dim|add)?(\d+)?(?:/([A-G])([#b]*))?$",
            RegexOptions.Compiled);

        public static string[] TransposeChords(string[] originalChords, int semitones)
        {
            var scale = DetermineScale(originalChords);
            return originalChords.Select(c => TransposeChord(c, semitones, scale)).ToArray();
        }

        public static string TransposeChord(string chord, int semitones, Dictionary<string, int>? scale = null)
        {
            if (string.IsNullOrWhiteSpace(chord)) return chord;
            chord = chord.Trim().Replace(" ", string.Empty);

            var match = _chordRegex.Match(chord);
            if (!match.Success) return chord;

            var rootNote = match.Groups[1].Value + match.Groups[2].Value;
            var chordQuality = match.Groups[3].Value + match.Groups[4].Value;
            var bassNote = match.Groups[5].Success
                ? match.Groups[5].Value + match.Groups[6].Value
                : string.Empty;

            // If no explicit scale, infer from this chord alone.
            scale ??= DetermineScale(new[] { chord });
            var preferFlat = PrefersFlat(scale);

            var transposedRoot = TransposeNote(rootNote, semitones, preferFlat);
            var transposedBass = string.IsNullOrEmpty(bassNote)
                ? string.Empty
                : TransposeNote(bassNote, semitones, preferFlat);

            return $"{transposedRoot}{chordQuality}" +
                   (string.IsNullOrEmpty(transposedBass) ? string.Empty : "/" + transposedBass);
        }

        /// <summary>
        /// Backward-compatible overload that takes a chromatic scale dict.
        /// New code should prefer the (note, semitones, preferFlat) overload.
        /// </summary>
        public static string TransposeNote(string note, int semitones, Dictionary<string, int> scale) =>
            TransposeNote(note, semitones, PrefersFlat(scale));

        public static string TransposeNote(string note, int semitones, bool preferFlat = false)
        {
            try
            {
                var semi = SemitoneOf(note);
                if (semi is null) return note;

                var newIndex = Mod12(semi.Value + semitones);
                return (preferFlat ? FlatScale : SharpScale)[newIndex];
            }
            catch
            {
                return note;
            }
        }

        /// <summary>
        /// Transposes a key root by <paramref name="semitones"/>, preserving the
        /// minor 'm' suffix. Spelling preference (sharp vs flat) follows the
        /// original input; falls back to sharp.
        /// </summary>
        public static string? TransposeKey(string? key, int semitones)
        {
            var semi = SemitoneOf(key);
            if (semi is null) return key;

            var preferFlat = key!.Contains('b');
            var root = (preferFlat ? FlatScale : SharpScale)[Mod12(semi.Value + semitones)];
            return IsMinor(key) ? root + "m" : root;
        }

        private static int Mod12(int n) => ((n % 12) + 12) % 12;
    }
}