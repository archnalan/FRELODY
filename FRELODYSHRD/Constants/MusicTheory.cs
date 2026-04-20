using System;
using System.Collections.Generic;
using System.Linq;

namespace FRELODYSHRD.Constants
{
    /// <summary>
    /// Shared music-theory primitives used across Compose (SongBoard) and Play
    /// (PlaylistDetail, Song player, ChordTransposer).
    ///
    /// Notes use the chromatic 12-tone system. Letters A–G are the natural notes;
    /// any number of '#' (sharp, +1 semitone) or 'b' (flat, -1 semitone) accidentals
    /// may follow, e.g. "C", "C#", "Db", "F##", "Abb". A trailing "m" denotes minor
    /// quality (irrelevant for the semitone index, retained by the helpers).
    /// </summary>
    public static class MusicTheory
    {
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

        /// <summary>
        /// Transposes a key root by <paramref name="semitones"/>, preserving the
        /// minor 'm' suffix. Spelling preference (sharp vs flat) follows the
        /// original input; falls back to sharp.
        /// </summary>
        public static string? Transpose(string? key, int semitones)
        {
            var semi = SemitoneOf(key);
            if (semi is null) return key;

            var preferFlat = key!.Contains('b');
            var newSemi = Mod12(semi.Value + semitones);
            var root = (preferFlat ? FlatScale : SharpScale)[newSemi];
            return IsMinor(key) ? root + "m" : root;
        }

        private static int Mod12(int n) => ((n % 12) + 12) % 12;
    }
}
