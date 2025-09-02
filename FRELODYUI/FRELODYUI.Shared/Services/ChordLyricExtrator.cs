using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FRELODYAPP.Interfaces;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.ModelTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UglyToad.PdfPig; // Assuming this is the PDF library used
using UglyToad.PdfPig.Content;

public class ChordLyricExtrator
{
    private int MAX_CHORD_LENGTH = 10; // Increased to handle longer chords like Cmaj7/E
    private static readonly Regex ChordRegex = new Regex(
        @"^([A-G])(#|b|bb|##)?(m|maj|min|sus|aug|dim|add)?(\d+)?(/([A-G])(#|b|bb|##)?)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex ChordPattern = new Regex(
        @"([A-G])(#|b|bb|##)?(m|maj|min|sus|aug|dim|add)?(\d+)?(/([A-G])(#|b|bb|##)?)?",
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

    private void AddPairedSegments(
    ICollection<SegmentCreateDto> songLyrics,
    List<Word> chordWords,
    List<Word> lyricWords,
    int lineNumber,
    ref int lyricOrder)
    {
        foreach (var lyric in lyricWords)
        {
            var match = chordWords
                .OrderBy(c => Math.Abs(c.BoundingBox.Left - lyric.BoundingBox.Left))
                .FirstOrDefault();

            string chord = null;
            if (match != null && Math.Abs(match.BoundingBox.Left - lyric.BoundingBox.Left) < 20) // tolerance in px
            {
                chord = match.Text;
            }

            songLyrics.Add(new SegmentCreateDto
            {
                Id = Guid.NewGuid().ToString(),
                Lyric = lyric.Text,
                LineNumber = lineNumber,
                ChordId = chord != null ? Guid.NewGuid().ToString() : null,
                ChordName = chord,
                PartNumber = 1,
                PartName = SongSection.Verse,
                LyricOrder = lyricOrder++,
                ChordAlignment = Alignment.Left
            });
        }
    }

    public SimpleSongCreateDto ExtractFromRawText(string rawText)
    {
        var song = new SimpleSongCreateDto
        {
            Title = string.Empty,
            SongLyrics = new List<SegmentCreateDto>()
        };

        var lines = rawText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        int lyricOrder = 1;
        int lineNumber = 1;
        bool titleSet = false;

        for (int i = 0; i < lines.Length;)
        {
            string current = lines[i].Trim();
            if (string.IsNullOrEmpty(current))
            {
                i++;
                continue;
            }

            if (!titleSet)
            {
                var match = TitleRegex.Match(current);
                if (match.Success)
                {
                    song.SongNumber = int.Parse(match.Groups["num"].Value);
                    song.Title = match.Groups["title"].Value.Trim();
                }
                else
                {
                    song.Title = current;
                }
                titleSet = true;
                i++;
                continue;
            }

            if (IsChordLineFromString(lines[i]))
            {
                if (i + 1 < lines.Length && IsLyricLineFromString(lines[i + 1]))
                {
                    AddPairedSegmentsFromText(song.SongLyrics, lines[i], lines[i + 1], lineNumber, ref lyricOrder);
                    lineNumber++;
                    i += 2;
                }
                else
                {
                    i++;
                }
            }
            else if (IsLyricLineFromString(lines[i]))
            {
                AddLyricsOnlyFromText(song.SongLyrics, lines[i], lineNumber, ref lyricOrder);
                lineNumber++;
                i++;
            }
            else
            {
                i++;
            }
        }

        if (string.IsNullOrEmpty(song.Title))
        {
            song.Title = "Untitled Song";
        }

        return song;
    }

    private bool IsChordLineFromString(string line)
    {
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 0 && words.All(IsChordWord);
    }

    private bool IsLyricLineFromString(string line)
    {
        return !IsChordLineFromString(line);
    }

    private void AddPairedSegmentsFromText(
        ICollection<SegmentCreateDto> songLyrics,
        string chordLine,
        string lyricLine,
        int lineNumber,
        ref int lyricOrder)
    {
        // Get chords with positions
        var chordMatches = ChordPattern.Matches(chordLine);
        List<(string chord, int start)> chords = new();
        foreach (Match m in chordMatches)
        {
            if (m.Length <= MAX_CHORD_LENGTH)
            {
                chords.Add((m.Value, m.Index));
            }
        }

        // Get lyric words with positions
        var wordRegex = new Regex(@"\S+");
        var wordMatches = wordRegex.Matches(lyricLine);
        foreach (Match wm in wordMatches)
        {
            string lyric = wm.Value;
            int wstart = wm.Index;

            // Check for inline [chord] first
            string chord = null;
            var inlineMatch = Regex.Match(lyric, @"^\[([^\]]+)\](.*)$");
            if (inlineMatch.Success)
            {
                string potentialChord = inlineMatch.Groups[1].Value;
                if (IsChordWord(potentialChord))
                {
                    chord = potentialChord;
                    lyric = inlineMatch.Groups[2].Value;
                }
            }

            // If no inline, find closest above chord
            if (chord == null && chords.Any())
            {
                var closest = chords.OrderBy(c => Math.Abs(c.start - wstart)).First();
                if (Math.Abs(closest.start - wstart) < 5) // Char tolerance
                {
                    chord = closest.chord;
                }
            }

            if (!string.IsNullOrEmpty(lyric))
            {
                songLyrics.Add(new SegmentCreateDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Lyric = lyric,
                    LineNumber = lineNumber,
                    ChordId = chord != null ? Guid.NewGuid().ToString() : null,
                    ChordName = chord,
                    PartNumber = 1,
                    PartName = SongSection.Verse,
                    LyricOrder = lyricOrder++,
                    ChordAlignment = Alignment.Left
                });
            }
        }
    }

    private void AddLyricsOnlyFromText(
        ICollection<SegmentCreateDto> songLyrics,
        string lyricLine,
        int lineNumber,
        ref int lyricOrder)
    {
        var wordRegex = new Regex(@"\S+");
        var wordMatches = wordRegex.Matches(lyricLine);
        foreach (Match wm in wordMatches)
        {
            string lyric = wm.Value;
            string chord = null;

            // Check for inline [chord]
            var inlineMatch = Regex.Match(lyric, @"^\[([^\]]+)\](.*)$");
            if (inlineMatch.Success)
            {
                string potentialChord = inlineMatch.Groups[1].Value;
                if (IsChordWord(potentialChord))
                {
                    chord = potentialChord;
                    lyric = inlineMatch.Groups[2].Value;
                }
            }

            if (!string.IsNullOrEmpty(lyric))
            {
                songLyrics.Add(new SegmentCreateDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Lyric = lyric,
                    LineNumber = lineNumber,
                    ChordId = chord != null ? Guid.NewGuid().ToString() : null,
                    ChordName = chord,
                    PartNumber = 1,
                    PartName = SongSection.Verse,
                    LyricOrder = lyricOrder++,
                    ChordAlignment = Alignment.Left
                });
            }
        }
    }

    public string ExtractTextFromDocx(string path)
    {
        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(path, false))
        {
            var body = wordDoc.MainDocumentPart.Document.Body;
            var sb = new System.Text.StringBuilder();
            foreach (var para in body.Elements<Paragraph>())
            {
                sb.AppendLine(para.InnerText);
            }
            return sb.ToString();
        }
    }
}

