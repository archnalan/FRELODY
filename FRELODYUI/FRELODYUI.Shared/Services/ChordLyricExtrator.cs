using FRELODYAPP.Interfaces;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.ModelTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FRELODYUI.Shared.Services
{
    public class ChordLyricExtrator
    {
        private int MAX_CHORD_LENGTH = 4;
        private static readonly Regex ChordRegex = new Regex(
            @"^([A-G])(#|b|bb|##)?(m|maj|min|sus|aug|dim|add)?(\d+)?(/([A-G])(#|b|bb|##)?)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private static readonly Regex TitleRegex = new Regex(
            @"^(?<num>\d+)(?:\.)?\s+(?<title>.+)$", // Handles "108 Amazing Grace" or "108. Amazing Grace"
            RegexOptions.Compiled
        );

        public SimpleSongCreateDto ExtractSong(string pdfPath)
        {
            var song = new SimpleSongCreateDto
            {
                Title = string.Empty,
                SongLyrics = new List<SegmentCreateDto>()
            };

            using (var pdf = PdfDocument.Open(pdfPath))
            {
                int lyricOrder = 1;
                int lineNumber = 1;

                foreach (var page in pdf.GetPages())
                {
                    var words = page.GetWords().ToList();

                    // Sort top-to-bottom (descending Top), left-to-right
                    words = words
                        .OrderByDescending(w => w.BoundingBox.Top)
                        .ThenBy(w => w.BoundingBox.Left)
                        .ToList();

                    // ---- 1. Group words into lines based on vertical proximity ----
                    var lines = GroupWordsIntoLines(words);

                    // ---- 2. Detect Title (top line on first page) ----
                    if (string.IsNullOrEmpty(song.Title) && page.Number == 1 && lines.Count > 0)
                    {
                        var titleLine = lines[0];
                        var rawTitle = string.Join(" ", titleLine.Select(w => w.Text)).Trim();

                        var match = TitleRegex.Match(rawTitle);
                        if (match.Success)
                        {
                            song.SongNumber = int.Parse(match.Groups["num"].Value);
                            song.Title = match.Groups["title"].Value.Trim();
                        }
                        else
                        {
                            song.Title = rawTitle;
                        }
                    }

                    // ---- 3. Extract chords + lyrics from non-title lines ----
                    int startLineIdx = (page.Number == 1) ? 1 : 0; // Skip title on first page
                    for (int i = startLineIdx; i < lines.Count;)
                    {
                        var currentLine = lines[i];

                        if (IsChordLine(currentLine))
                        {
                            if (i + 1 < lines.Count && IsLyricLine(lines[i + 1]))
                            {
                                // Pair chord line with next lyric line
                                var chordLine = currentLine;
                                var lyricLine = lines[i + 1];

                                // Assign chords to lyrics based on horizontal positions
                                AddPairedSegments(song.SongLyrics, chordLine, lyricLine, lineNumber, ref lyricOrder);

                                lineNumber++; // Increment for the lyric line (chord line doesn't count as a separate lyric line)
                                i += 2; // Skip the paired lines
                            }
                            else
                            {
                                // Chord line without following lyrics: ignore or handle as needed (e.g., add as special segment)
                                i++;
                            }
                        }
                        else if (IsLyricLine(currentLine))
                        {
                            // Lyric line without chords: add each word as lyric-only
                            foreach (var word in currentLine)
                            {
                                song.SongLyrics.Add(new SegmentCreateDto
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Lyric = word.Text,
                                    LineNumber = lineNumber,
                                    ChordId = null,
                                    ChordName = null,
                                    PartNumber = 1,
                                    PartName = SongSection.Verse,
                                    LyricOrder = lyricOrder++,
                                    ChordAlignment = Alignment.Left
                                });
                            }
                            lineNumber++;
                            i++;
                        }
                        else
                        {
                            // Mixed or unknown line: skip or log
                            i++;
                        }
                    }
                }
            }

            return song;
        }

        // Helper: Group words into lines using vertical tolerance (adjust tolerance based on font sizes)
        private List<List<Word>> GroupWordsIntoLines(List<Word> words)
        {
            var lines = new List<List<Word>>();
            if (words.Count == 0) return lines;

            var currentLine = new List<Word> { words[0] };
            double prevY = (words[0].BoundingBox.Top + words[0].BoundingBox.Bottom) / 2;

            for (int i = 1; i < words.Count; i++)
            {
                double currentY = (words[i].BoundingBox.Top + words[i].BoundingBox.Bottom) / 2;
                if (Math.Abs(currentY - prevY) > 10) // Tolerance for line break (adjust based on PDF)
                {
                    lines.Add(currentLine.OrderBy(w => w.BoundingBox.Left).ToList()); // Sort words left-to-right
                    currentLine = new List<Word>();
                }
                currentLine.Add(words[i]);
                prevY = currentY;
            }
            if (currentLine.Count > 0)
            {
                lines.Add(currentLine.OrderBy(w => w.BoundingBox.Left).ToList());
            }
            return lines;
        }
        private bool IsChordWord(string text)
        {
            return ChordRegex.IsMatch(text) && text.Length <= MAX_CHORD_LENGTH;
        }
        private bool IsChordLine(List<Word> line)
        {
            return line.Count > 0 && line.All(w => IsChordWord(w.Text));
        }

        private bool IsLyricLine(List<Word> line)
        {
            return !IsChordLine(line);
        }

        // Helper: Add segments for paired chord and lyric lines using position-based assignment
        private void AddPairedSegments(ICollection<SegmentCreateDto> songLyrics, List<Word> chordWords, List<Word> lyricWords, int lineNumber, ref int lyricOrder)
        {
            // Create events for chords and lyrics based on left position
            var events = chordWords.Select(w => new { Pos = w.BoundingBox.Left, Type = "chord", Text = w.Text })
                .Concat(lyricWords.Select(w => new { Pos = w.BoundingBox.Left, Type = "lyric", Text = w.Text }))
                .OrderBy(e => e.Pos)
                .ThenBy(e => e.Type == "chord" ? 0 : 1) // Chords before lyrics at same position
                .ToList();

            string currentChord = null;
            foreach (var evt in events)
            {
                if (evt.Type == "chord")
                {
                    currentChord = evt.Text;
                }
                else if (evt.Type == "lyric")
                {
                    songLyrics.Add(new SegmentCreateDto
                    {
                        Id = Guid.NewGuid().ToString(),
                        Lyric = evt.Text,
                        LineNumber = lineNumber,
                        ChordId = currentChord != null ? Guid.NewGuid().ToString() : null,
                        ChordName = currentChord,
                        PartNumber = 1,
                        PartName = SongSection.Verse,
                        LyricOrder = lyricOrder++,
                        ChordAlignment = Alignment.Left
                    });
                }
            }
        }
    }
}
