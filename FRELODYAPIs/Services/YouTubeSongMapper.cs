using FRELODYAPP.Interfaces;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.ModelTypes;

namespace FRELODYAPIs.Services
{
    /// <summary>
    /// Maps an analyzed YouTube transcription into the <see cref="SimpleSongCreateDto"/>
    /// shape consumed by <c>SongService.CreateSong</c>. With synced lyrics, chords are
    /// placed over the words they sound under (proportional position inside each lyric
    /// line's time window) and instrumental gaps become chord-only lines. Without lyrics,
    /// each detected chord change becomes a chord-only segment; consecutive identical
    /// chords are collapsed, and beats are grouped into measures/lines using the detected
    /// time signature.
    /// </summary>
    public static class YouTubeSongMapper
    {
        // How many measures fall on one chart line before wrapping to the next.
        private const int MeasuresPerLine = 4;

        // A lyric line with no successor keeps collecting chords for this long before the
        // rest spill into a trailing chord-only outro line.
        private const double LastLineWindowSeconds = 8;

        public static SimpleSongCreateDto Map(
            YouTubeTranscriptionDto transcription,
            string title,
            IReadOnlyList<FRELODYAPIs.Services.ChordMini.LyricsLine>? syncedLyrics = null)
        {
            var hasLyrics = syncedLyrics is { Count: > 0 } &&
                            syncedLyrics.Any(l => !string.IsNullOrWhiteSpace(l.Text));
            return new SimpleSongCreateDto
            {
                Title = string.IsNullOrWhiteSpace(title) ? "Untitled" : title.Trim(),
                Key = NormalizeKey(transcription.KeySignature),
                OriginalKey = NormalizeKey(transcription.KeySignature),
                SongLyrics = hasLyrics
                    ? BuildLyricAlignedSegments(transcription, syncedLyrics!)
                    : BuildSegments(transcription)
            };
        }

        /// <summary>
        /// Best-effort "Artist - Title" split of a video title, with the usual decorations
        /// ("(Official Video)", "[Lyrics]", "| HD" …) stripped. Returns null artist when the
        /// title carries no separator — callers then fall back to a free-text lyrics search.
        /// </summary>
        public static (string? Artist, string Title) ParseArtistTitle(string videoTitle)
        {
            var cleaned = System.Text.RegularExpressions.Regex.Replace(
                videoTitle,
                @"[\(\[][^)\]]*(official|video|audio|lyric|lyrics|visualizer|hd|hq|4k|mv|live|remaster)[^)\]]*[\)\]]",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            cleaned = cleaned.Split('|')[0].Trim().Trim('-').Trim();

            var dash = cleaned.IndexOf(" - ", StringComparison.Ordinal);
            if (dash <= 0)
                return (null, cleaned.Length > 0 ? cleaned : videoTitle);

            var artist = cleaned[..dash].Trim();
            var song = cleaned[(dash + 3)..].Trim();
            return (artist.Length > 0 && song.Length > 0) ? (artist, song) : (null, cleaned);
        }

        // ── Lyric-aligned chart ─────────────────────────────────────────────
        //
        // LRCLib gives line-level timestamps; chord events have onset times. Each lyric
        // line owns the window [its time, next line's time). Chords inside the window are
        // pinned to the word at the chord's proportional position in the window, producing
        // the familiar chords-over-words chart. Windows with no words (intro, solos, gaps —
        // LRCLib emits empty-text lines there) become chord-only lines so the progression
        // is never silently dropped.
        private static List<SegmentCreateDto> BuildLyricAlignedSegments(
            YouTubeTranscriptionDto t,
            IReadOnlyList<FRELODYAPIs.Services.ChordMini.LyricsLine> lyrics)
        {
            var segments = new List<SegmentCreateDto>();

            // Collapse chord runs into change events once, keeping onset times.
            var changes = new List<(double Time, string? Name)>(); // Name null = rest
            string? prevKey = null;
            foreach (var sc in (t.SyncedChords ?? []).OrderBy(s => s.Time))
            {
                var name = sc.Chord?.Trim();
                var key = IsRest(name) ? "<rest>" : name!;
                if (key == prevKey) continue;
                prevKey = key;
                changes.Add((sc.Time, key == "<rest>" ? null : key));
            }
            if (changes.Count == 0)
                return BuildSegments(t);

            // Window boundaries: every LRCLib line (empty ones delimit instrumental gaps),
            // preceded by a virtual t=0 gap window and capped past the last chord.
            var ordered = lyrics.OrderBy(l => l.Time).ToList();
            var lastEdge = Math.Max(
                changes[^1].Time + 1,
                ordered[^1].Time + LastLineWindowSeconds);

            var windows = new List<(double Start, double End, string Text)>();
            if (ordered[0].Time > 0)
                windows.Add((0, ordered[0].Time, string.Empty));
            for (var i = 0; i < ordered.Count; i++)
            {
                var end = i + 1 < ordered.Count ? ordered[i + 1].Time : lastEdge;
                windows.Add((ordered[i].Time, end, ordered[i].Text?.Trim() ?? string.Empty));
            }

            var lineNumber = 0;
            foreach (var (start, end, text) in windows)
            {
                var windowChords = changes
                    .Where(c => c.Time >= start && c.Time < end)
                    .ToList();

                if (string.IsNullOrEmpty(text))
                {
                    // Instrumental window — keep the progression as a chord-only line
                    // (rest cells included: they carry the spacing of the passage).
                    if (windowChords.All(c => c.Name is null)) continue;
                    var order = 0;
                    foreach (var (_, name) in windowChords)
                    {
                        segments.Add(NewSegment(
                            lyric: string.Empty, lineNumber, order++,
                            chordName: name));
                    }
                    lineNumber++;
                    continue;
                }

                var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var sung = windowChords.Where(c => c.Name is not null).ToList();
                if (sung.Count == 0 || words.Length == 0)
                {
                    segments.Add(NewSegment(text, lineNumber++, 0, chordName: null));
                    continue;
                }

                // Pin each chord to a word by its relative position in the window;
                // later chords can't land before earlier ones.
                var span = Math.Max(end - start, 0.001);
                var pins = new List<(int WordIndex, string Name)>();
                foreach (var (time, name) in sung)
                {
                    var idx = (int)Math.Floor((time - start) / span * words.Length);
                    idx = Math.Clamp(idx, 0, words.Length - 1);
                    if (pins.Count > 0 && idx <= pins[^1].WordIndex)
                        idx = Math.Min(pins[^1].WordIndex + 1, words.Length - 1);
                    if (pins.Count > 0 && idx == pins[^1].WordIndex)
                        pins[^1] = (idx, name!); // same word — last chord wins
                    else
                        pins.Add((idx, name!));
                }

                // Cut the line at each pinned word; a chord-less prefix keeps the words
                // readable when the first chord lands mid-line.
                var orderInLine = 0;
                if (pins[0].WordIndex > 0)
                {
                    segments.Add(NewSegment(
                        string.Join(' ', words[..pins[0].WordIndex]) + " ",
                        lineNumber, orderInLine++, chordName: null));
                }
                for (var p = 0; p < pins.Count; p++)
                {
                    var from = pins[p].WordIndex;
                    var to = p + 1 < pins.Count ? pins[p + 1].WordIndex : words.Length;
                    var chunk = string.Join(' ', words[from..to]);
                    if (p + 1 < pins.Count) chunk += " ";
                    segments.Add(NewSegment(chunk, lineNumber, orderInLine++, pins[p].Name));
                }
                lineNumber++;
            }

            return segments.Count > 0 ? segments : BuildSegments(t);
        }

        private static SegmentCreateDto NewSegment(
            string lyric, int lineNumber, int lyricOrder, string? chordName)
        {
            return new SegmentCreateDto
            {
                Id = Guid.NewGuid().ToString(),
                Lyric = lyric,
                LineNumber = lineNumber,
                ChordId = chordName is null ? null : Guid.NewGuid().ToString(),
                ChordName = chordName,
                PartNumber = (int)SongSection.unknown,
                PartName = SongSection.unknown,
                LyricOrder = lyricOrder,
                ChordAlignment = Alignment.Left
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
