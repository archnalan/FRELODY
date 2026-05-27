using FRELODYAPP.Interfaces;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.ModelTypes;

namespace FRELODYAPIs.Services
{
    /// <summary>
    /// Maps an analyzed YouTube transcription into the <see cref="SimpleSongCreateDto"/>
    /// shape consumed by <c>SongService.CreateSong</c>. Each detected chord change becomes
    /// a chord-only lyric segment; consecutive identical chords are collapsed, and beats are
    /// grouped into measures/lines using the detected time signature.
    /// </summary>
    public static class YouTubeSongMapper
    {
        // How many measures fall on one chart line before wrapping to the next.
        private const int MeasuresPerLine = 4;

        public static SimpleSongCreateDto Map(YouTubeTranscriptionDto transcription, string title)
        {
            return new SimpleSongCreateDto
            {
                Title = string.IsNullOrWhiteSpace(title) ? "Untitled" : title.Trim(),
                Key = NormalizeKey(transcription.KeySignature),
                OriginalKey = NormalizeKey(transcription.KeySignature),
                SongLyrics = BuildSegments(transcription)
            };
        }

        private static List<SegmentCreateDto> BuildSegments(YouTubeTranscriptionDto t)
        {
            var segments = new List<SegmentCreateDto>();
            if (t.SyncedChords is null || t.SyncedChords.Count == 0)
                return segments;

            var beatsPerMeasure = ParseBeatsPerMeasure(t.TimeSignature);
            var beatsPerLine = Math.Max(1, beatsPerMeasure * MeasuresPerLine);

            var currentLine = -1;
            string? prevKey = null;
            var orderInLine = 0;

            foreach (var sc in t.SyncedChords.OrderBy(s => s.BeatIndex))
            {
                var lineNumber = sc.BeatIndex / beatsPerLine;

                // Re-emit the active chord at the start of each new line so every line is
                // self-contained (and reset the per-line ordering).
                if (lineNumber != currentLine)
                {
                    currentLine = lineNumber;
                    prevKey = null;
                    orderInLine = 0;
                }

                var chordName = sc.Chord?.Trim();
                var isRest = IsRest(chordName);

                // Collapse runs of the same chord (and runs of rests) into a single cell.
                var key = isRest ? "<rest>" : chordName!;
                if (key == prevKey)
                    continue;
                prevKey = key;

                segments.Add(new SegmentCreateDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Lyric = string.Empty,
                    LineNumber = lineNumber,
                    // A non-empty ChordId flags this segment for chord creation/linking in
                    // CreateSong (which overwrites it with the real id). Null = rest cell.
                    ChordId = isRest ? null : Guid.NewGuid().ToString(),
                    ChordName = isRest ? null : chordName,
                    PartNumber = (int)SongSection.unknown,
                    PartName = SongSection.unknown,
                    LyricOrder = orderInLine++,
                    ChordAlignment = Alignment.Left
                });
            }

            return segments;
        }

        private static int ParseBeatsPerMeasure(string? timeSignature)
        {
            if (!string.IsNullOrEmpty(timeSignature))
            {
                var slash = timeSignature.IndexOf('/');
                if (slash > 0 && int.TryParse(timeSignature[..slash], out var n) && n is >= 2 and <= 12)
                    return n;
            }
            return 4;
        }

        // ChordMini emits "N"/"X" (and variants) for no-chord/rest spans.
        private static bool IsRest(string? chord)
        {
            if (string.IsNullOrWhiteSpace(chord))
                return true;
            var c = chord.Trim();
            return c is "N" or "X" or "NC" or "N.C."
                || c.Equals("none", StringComparison.OrdinalIgnoreCase);
        }

        // KeySignature arrives as e.g. "C major" / "A minor". Convert to chart-friendly "C" / "Am".
        private static string? NormalizeKey(string? keySignature)
        {
            if (string.IsNullOrWhiteSpace(keySignature))
                return null;
            var k = keySignature.Trim();
            if (k.EndsWith(" minor", StringComparison.OrdinalIgnoreCase))
                return k[..^" minor".Length].Trim() + "m";
            if (k.EndsWith(" major", StringComparison.OrdinalIgnoreCase))
                return k[..^" major".Length].Trim();
            return k;
        }
    }
}
