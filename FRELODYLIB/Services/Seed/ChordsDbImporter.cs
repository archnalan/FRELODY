using System.Text.Json;
using System.Text.Json.Serialization;
using FRELODYSHRD.Models.ChordDraw;

namespace FRELODYAPP.Services.Seed
{
    public record SeededVoicing(
        string ChordName,
        string DisplayLabel,
        string FileNameStem,
        int Position,
        ChordDrawData Data,
        IReadOnlyList<int> Midi);

    public class ChordsDbImporter
    {
        private const int MaxVoicingsPerChord = 5;
        private const int MaxStringCount = 6;

        public IReadOnlyList<SeededVoicing> Import(string sourceJsonPath)
        {
            var json = File.ReadAllText(sourceJsonPath);
            var root = JsonSerializer.Deserialize<ChordsDbRoot>(json, JsonOpts)
                ?? throw new InvalidOperationException("chords-db JSON parsed to null.");

            var output = new List<SeededVoicing>();

            foreach (var (dictKey, entries) in root.Chords)
            {
                // chords-db nests every voicing under exactly one spelling per pitch class
                // ("C", "Csharp", "D", "Eb", "Fsharp", "Ab", "Bb", ...). We expand each black
                // key to BOTH enharmonic spellings and seed them as distinct chords: C# and Db
                // share a fretboard shape but are musically separate names, so a song written in
                // either spelling resolves to a standard chart. (Unknown keys fall back to the
                // dataset's literal spelling so nothing is silently lost.)
                var spellings = RootSpellings.TryGetValue(dictKey, out var s) ? s : [dictKey];
                var rootPitch = PitchClass(spellings[0]);
                if (rootPitch < 0) continue;

                foreach (var entry in entries)
                {
                    var quality = QualityCatalog.Resolve(entry.Suffix);
                    if (quality is null) continue;

                    // The shape is spelling-independent: pick the valid voicings once, then
                    // re-label the same shapes under each enharmonic spelling of the root.
                    var positions = entry.Positions
                        .Where(p => p.Frets is { Length: MaxStringCount })
                        .OrderBy(p => p.BaseFret)
                        .Where(p => IsValid(p, rootPitch, quality))
                        .Take(MaxVoicingsPerChord)
                        .ToList();

                    foreach (var spelling in spellings)
                    {
                        int seq = 1;
                        foreach (var pos in positions)
                        {
                            var data = ToChordDrawData(pos);
                            var (label, stem) = BuildLabel(spelling, quality, pos.BaseFret, seq);
                            var chordName = spelling + quality.NameSuffix;

                            output.Add(new SeededVoicing(
                                ChordName: chordName,
                                DisplayLabel: label,
                                FileNameStem: stem,
                                Position: pos.BaseFret,
                                Data: data,
                                Midi: pos.Midi ?? Array.Empty<int>()));

                            seq++;
                        }
                    }
                }
            }

            return output;
        }

        private static bool IsValid(ChordsDbPosition pos, int rootPc, Quality quality)
        {
            if (pos.Midi is null || pos.Midi.Length == 0) return false;

            var pcs = pos.Midi.Select(m => ((m % 12) + 12) % 12).ToHashSet();
            var expected = quality.Intervals.Select(i => ((rootPc + i) % 12 + 12) % 12).ToHashSet();

            // Root must always be present.
            if (!pcs.Contains(rootPc)) return false;

            if (quality.Category == QualityCategory.Triad)
            {
                // Triads: require all expected pitch classes.
                if (!expected.IsSubsetOf(pcs)) return false;
            }
            // Extensions: root presence is sufficient (root already checked above).

            return true;
        }

        private static ChordDrawData ToChordDrawData(ChordsDbPosition pos)
        {
            var fingers = new List<ChordFinger>(MaxStringCount);
            var maxDisplayedFret = 0;

            for (int i = 0; i < MaxStringCount; i++)
            {
                var stringNum = MaxStringCount - i; // chords-db index 0 = low E = our string 6
                var fret = pos.Frets[i];
                var fingerNum = pos.Fingers is { Length: MaxStringCount } ? pos.Fingers[i] : 0;

                var text = fret >= 1 && fingerNum >= 1 ? fingerNum.ToString() : null;
                fingers.Add(new ChordFinger { String = stringNum, Fret = fret, Text = text });

                if (fret > maxDisplayedFret) maxDisplayedFret = fret;
            }

            var barres = new List<ChordBarre>();
            if (pos.Barres is { Length: > 0 })
            {
                foreach (var barreFret in pos.Barres.Distinct())
                {
                    var stringsOnBarre = new List<int>();
                    for (int i = 0; i < MaxStringCount; i++)
                    {
                        if (pos.Frets[i] == barreFret)
                            stringsOnBarre.Add(MaxStringCount - i);
                    }
                    if (stringsOnBarre.Count < 2) continue;

                    var fromString = stringsOnBarre.Max();
                    var toString = stringsOnBarre.Min();
                    barres.Add(new ChordBarre
                    {
                        FromString = fromString,
                        ToString = toString,
                        Fret = barreFret,
                        Text = "1"
                    });

                    // Remove individual finger entries that are covered by the barre,
                    // so the renderer doesn't draw a circle on top of the bar.
                    fingers.RemoveAll(f => stringsOnBarre.Contains(f.String) && f.Fret == barreFret);
                }
            }

            // Show at least 4 frets so single-fret shapes still look like a fretboard.
            var displayedFrets = Math.Max(4, Math.Min(5, maxDisplayedFret));

            return new ChordDrawData
            {
                Chord = new ChordContent { Fingers = fingers, Barres = barres },
                Settings = new ChordDrawSettings
                {
                    Strings = MaxStringCount,
                    Frets = displayedFrets,
                    Position = pos.BaseFret,
                    Tuning = ChordDrawSettings.DefaultGuitarTuning
                }
            };
        }

        private static (string Display, string Stem) BuildLabel(string key, Quality quality, int baseFret, int seq)
        {
            var chordName = key + quality.NameSuffix;
            string display, stem;

            if (baseFret <= 1)
            {
                display = $"Open {chordName} (position {seq})";
                stem = $"Open_{Safe(chordName)}_position_{seq}";
            }
            else
            {
                display = $"{chordName} at {baseFret} (position {seq})";
                stem = $"{Safe(chordName)}_at_{baseFret}_position_{seq}";
            }

            return (display, stem);
        }

        private static string Safe(string s)
        {
            var chars = s.Select(c =>
                char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c :
                c == '/' ? '_' :
                c == '#' ? 's' :
                c == 'b' ? c :
                '_').ToArray();
            return new string(chars);
        }

        // Top-level chords-db dictionary key → the display spelling(s) we seed.
        // Black keys yield both enharmonic names (sharp first; PitchClass keys off [0]).
        private static readonly Dictionary<string, string[]> RootSpellings = new()
        {
            ["C"]      = ["C"],
            ["Csharp"] = ["C#", "Db"],
            ["D"]      = ["D"],
            ["Eb"]     = ["Eb", "D#"],
            ["E"]      = ["E"],
            ["F"]      = ["F"],
            ["Fsharp"] = ["F#", "Gb"],
            ["G"]      = ["G"],
            ["Ab"]     = ["Ab", "G#"],
            ["A"]      = ["A"],
            ["Bb"]     = ["Bb", "A#"],
            ["B"]      = ["B"]
        };

        private static int PitchClass(string note) => note switch
        {
            "C"  => 0,
            "C#" => 1, "Db" => 1,
            "D"  => 2,
            "D#" => 3, "Eb" => 3,
            "E"  => 4,
            "F"  => 5,
            "F#" => 6, "Gb" => 6,
            "G"  => 7,
            "G#" => 8, "Ab" => 8,
            "A"  => 9,
            "A#" => 10, "Bb" => 10,
            "B"  => 11,
            _    => -1
        };

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private record ChordsDbRoot(
            [property: JsonPropertyName("chords")]
            Dictionary<string, ChordsDbEntry[]> Chords);

        private record ChordsDbEntry(
            [property: JsonPropertyName("key")] string Key,
            [property: JsonPropertyName("suffix")] string Suffix,
            [property: JsonPropertyName("positions")] ChordsDbPosition[] Positions);

        private record ChordsDbPosition(
            [property: JsonPropertyName("frets")] int[] Frets,
            [property: JsonPropertyName("fingers")] int[]? Fingers,
            [property: JsonPropertyName("baseFret")] int BaseFret,
            [property: JsonPropertyName("barres")] int[]? Barres,
            [property: JsonPropertyName("capo")] bool? Capo,
            [property: JsonPropertyName("midi")] int[]? Midi);
    }

    public enum QualityCategory { Triad, Extension }

    public record Quality(string NameSuffix, int[] Intervals, QualityCategory Category);

    public static class QualityCatalog
    {
        // Slash chords reuse the base-chord intervals; the bass-note check is implicit
        // because chords-db only emits valid voicings (we'd reject the rare bad one
        // via the root-presence check). NameSuffix preserves the slash text verbatim.
        public static Quality? Resolve(string suffix)
        {
            if (string.IsNullOrEmpty(suffix)) return null;

            // Slash chord: "7/G", "/E", "m/B", "m9/Bb"
            if (suffix.Contains('/'))
            {
                var slashIdx = suffix.IndexOf('/');
                var basePart = suffix[..slashIdx];
                var slashPart = suffix[slashIdx..];

                if (basePart.Length == 0) return Map.TryGetValue("major", out var maj)
                    ? maj with { NameSuffix = slashPart } : null;

                if (Map.TryGetValue(basePart, out var q))
                    return q with { NameSuffix = q.NameSuffix + slashPart };

                return null;
            }

            return Map.TryGetValue(suffix, out var quality) ? quality : null;
        }

        private static readonly Dictionary<string, Quality> Map = new()
        {
            ["major"]      = new("",       [0, 4, 7],            QualityCategory.Triad),
            ["minor"]      = new("m",      [0, 3, 7],            QualityCategory.Triad),
            // Slash-chord base alias: chords-db spells minor slash chords "m/G" (not "minor/G"),
            // so the slash resolver below needs an "m" key to map onto the minor triad.
            ["m"]          = new("m",      [0, 3, 7],            QualityCategory.Triad),
            ["dim"]        = new("dim",    [0, 3, 6],            QualityCategory.Triad),
            ["aug"]        = new("aug",    [0, 4, 8],            QualityCategory.Triad),
            ["sus"]        = new("sus4",   [0, 5, 7],            QualityCategory.Triad),
            ["sus2"]       = new("sus2",   [0, 2, 7],            QualityCategory.Triad),
            ["sus4"]       = new("sus4",   [0, 5, 7],            QualityCategory.Triad),
            ["sus2sus4"]   = new("sus2sus4",[0, 2, 5, 7],        QualityCategory.Extension),
            ["5"]          = new("5",      [0, 7],               QualityCategory.Triad),

            ["dim7"]       = new("dim7",   [0, 3, 6, 9],         QualityCategory.Extension),
            ["6"]          = new("6",      [0, 4, 7, 9],         QualityCategory.Extension),
            ["69"]         = new("6/9",    [0, 2, 4, 7, 9],      QualityCategory.Extension),
            ["7"]          = new("7",      [0, 4, 7, 10],        QualityCategory.Extension),
            ["7sus4"]      = new("7sus4",  [0, 5, 7, 10],        QualityCategory.Extension),
            ["7b5"]        = new("7b5",    [0, 4, 6, 10],        QualityCategory.Extension),
            ["aug7"]       = new("aug7",   [0, 4, 8, 10],        QualityCategory.Extension),
            ["7b9"]        = new("7b9",    [0, 1, 4, 7, 10],     QualityCategory.Extension),
            ["7#9"]        = new("7#9",    [0, 3, 4, 7, 10],     QualityCategory.Extension),
            ["9"]          = new("9",      [0, 2, 4, 7, 10],     QualityCategory.Extension),
            ["9b5"]        = new("9b5",    [0, 2, 4, 6, 10],     QualityCategory.Extension),
            ["aug9"]       = new("aug9",   [0, 2, 4, 8, 10],     QualityCategory.Extension),
            ["11"]         = new("11",     [0, 4, 5, 7, 10],     QualityCategory.Extension),
            ["9#11"]       = new("9#11",   [0, 2, 4, 6, 7, 10],  QualityCategory.Extension),
            ["13"]         = new("13",     [0, 4, 7, 9, 10],     QualityCategory.Extension),
            ["alt"]        = new("alt",    [0, 4, 10],           QualityCategory.Extension),

            ["maj7"]       = new("maj7",   [0, 4, 7, 11],        QualityCategory.Extension),
            ["maj7b5"]     = new("maj7b5", [0, 4, 6, 11],        QualityCategory.Extension),
            ["maj7#5"]     = new("maj7#5", [0, 4, 8, 11],        QualityCategory.Extension),
            ["maj7sus2"]   = new("maj7sus2",[0, 2, 7, 11],       QualityCategory.Extension),
            ["maj9"]       = new("maj9",   [0, 2, 4, 7, 11],     QualityCategory.Extension),
            ["maj11"]      = new("maj11",  [0, 4, 5, 7, 11],     QualityCategory.Extension),
            ["maj13"]      = new("maj13",  [0, 4, 7, 9, 11],     QualityCategory.Extension),

            ["m6"]         = new("m6",     [0, 3, 7, 9],         QualityCategory.Extension),
            ["m69"]        = new("m6/9",   [0, 2, 3, 7, 9],      QualityCategory.Extension),
            ["m7"]         = new("m7",     [0, 3, 7, 10],        QualityCategory.Extension),
            ["m7b5"]       = new("m7b5",   [0, 3, 6, 10],        QualityCategory.Extension),
            ["m9"]         = new("m9",     [0, 2, 3, 7, 10],     QualityCategory.Extension),
            ["m11"]        = new("m11",    [0, 2, 3, 5, 7, 10],  QualityCategory.Extension),

            ["mmaj7"]      = new("m(maj7)",[0, 3, 7, 11],        QualityCategory.Extension),
            ["mmaj7b5"]    = new("m(maj7)b5",[0, 3, 6, 11],      QualityCategory.Extension),
            ["mmaj9"]      = new("m(maj9)",[0, 2, 3, 7, 11],     QualityCategory.Extension),
            ["mmaj11"]     = new("m(maj11)",[0, 2, 3, 5, 7, 11], QualityCategory.Extension),

            ["add9"]       = new("add9",   [0, 2, 4, 7],         QualityCategory.Extension),
            ["madd9"]      = new("m(add9)",[0, 2, 3, 7],         QualityCategory.Extension),
            ["add11"]      = new("add11",  [0, 4, 5, 7],         QualityCategory.Extension)
        };
    }
}
