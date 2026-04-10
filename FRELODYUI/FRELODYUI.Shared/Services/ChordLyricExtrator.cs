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
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

public enum SongFormat
{
    InlineChord,
    AlternatingChordLyric,
    Unknown
}

public class ChordLyricExtrator
{
    private const int MAX_CHORD_LENGTH = 10;

    private static readonly Regex ChordRegex = new Regex(
        @"^([A-G])(#|b|bb|##)?(m|maj|min|sus|aug|dim|add)?(\d+)?(/([A-G])(#|b|bb|##)?)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex ChordPattern = new Regex(
        @"([A-G])(#|b|bb|##)?(m|maj|min|sus|aug|dim|add)?(\d+)?(/([A-G])(#|b|bb|##)?)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex TitleRegex = new Regex(
        @"^(?<num>\d+)(?:\.)?\s+(?<title>.+)$",
        RegexOptions.Compiled
    );

    private static readonly Regex InlineBracketChordRegex = new Regex(
        @"\[([A-G][#b]?(?:m|maj|min|sus|aug|dim|add)?\d?(?:/[A-G][#b]?)?)\s*\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex InlineChordSegmentRegex = new Regex(
        @"\[([^\]]+)\]([^\[]*)",
        RegexOptions.Compiled
    );

    // ──────────────────────────────────────────────
    //  PDF Extraction (unchanged logic)
    // ──────────────────────────────────────────────

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
                words = words
                    .OrderByDescending(w => w.BoundingBox.Top)
                    .ThenBy(w => w.BoundingBox.Left)
                    .ToList();

                var lines = GroupWordsIntoLines(words);

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

                int startLineIdx = (page.Number == 1) ? 1 : 0;
                for (int i = startLineIdx; i < lines.Count;)
                {
                    var currentLine = lines[i];

                    if (IsChordLinePdf(currentLine))
                    {
                        if (i + 1 < lines.Count && !IsChordLinePdf(lines[i + 1]))
                        {
                            AddPairedSegmentsPdf(song.SongLyrics, currentLine, lines[i + 1], lineNumber, ref lyricOrder);
                            lineNumber++;
                            i += 2;
                        }
                        else
                        {
                            i++;
                        }
                    }
                    else
                    {
                        foreach (var word in currentLine)
                        {
                            song.SongLyrics.Add(new SegmentCreateDto
                            {
                                Id = Guid.NewGuid().ToString(),
                                Lyric = word.Text,
                                LineNumber = lineNumber,
                                PartNumber = 0,
                                PartName = SongSection.unknown,
                                LyricOrder = lyricOrder++,
                                ChordAlignment = Alignment.Left
                            });
                        }
                        lineNumber++;
                        i++;
                    }
                }
            }
        }

        return song;
    }

    // ──────────────────────────────────────────────
    //  Raw Text Extraction (multi-format)
    // ──────────────────────────────────────────────

    public SimpleSongCreateDto ExtractFromRawText(string rawText)
    {
        var song = new SimpleSongCreateDto
        {
            Title = string.Empty,
            SongLyrics = new List<SegmentCreateDto>()
        };

        if (string.IsNullOrWhiteSpace(rawText))
        {
            song.Title = "Untitled Song";
            return song;
        }

        var lines = rawText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        // Step 1: Extract title and song number
        int startIndex = ExtractTitleAndNumber(lines, song);

        // Step 2: Detect format from remaining lines
        var format = DetectFormat(lines, startIndex);

        // Step 3: Parse based on detected format
        int lineNumber = 1;
        int lyricOrder = 1;
        switch (format)
        {
            case SongFormat.InlineChord:
                ParseInlineFormat(lines, startIndex, song.SongLyrics, ref lineNumber, ref lyricOrder);
                break;
            case SongFormat.AlternatingChordLyric:
                ParseAlternatingFormat(lines, startIndex, song.SongLyrics, ref lineNumber, ref lyricOrder);
                break;
            default:
                ParsePlainLyrics(lines, startIndex, song.SongLyrics, ref lineNumber, ref lyricOrder);
                break;
        }

        // Step 4: Fallback — if title extraction consumed all usable content, re-parse from index 0.
        // This handles cases like a single lyric line ("glory glory hallelujah") or a single
        // chord+lyric pair ("A\nAmazing grace") where the lyric was wrongly consumed as the title.
        if (song.SongLyrics.Count == 0 && startIndex > 0)
        {
            lineNumber = 1;
            lyricOrder = 1;
            var fullFormat = DetectFormat(lines, 0);
            switch (fullFormat)
            {
                case SongFormat.InlineChord:
                    ParseInlineFormat(lines, 0, song.SongLyrics, ref lineNumber, ref lyricOrder);
                    break;
                case SongFormat.AlternatingChordLyric:
                    ParseAlternatingFormat(lines, 0, song.SongLyrics, ref lineNumber, ref lyricOrder);
                    break;
                default:
                    ParsePlainLyrics(lines, 0, song.SongLyrics, ref lineNumber, ref lyricOrder);
                    break;
            }
        }

        if (string.IsNullOrEmpty(song.Title))
            song.Title = "Untitled Song";

        return song;
    }

    // ──────────────────────────────────────────────
    //  Title & Number Extraction
    // ──────────────────────────────────────────────

    private int ExtractTitleAndNumber(string[] lines, SimpleSongCreateDto song)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            // Skip lines that look like pure chord lines or bracket-chord lines
            if (IsChordLineFromString(trimmed) || InlineBracketChordRegex.IsMatch(trimmed))
                continue;

            var match = TitleRegex.Match(trimmed);
            if (match.Success)
            {
                song.SongNumber = int.Parse(match.Groups["num"].Value);
                song.Title = match.Groups["title"].Value.Trim();
            }
            else
            {
                song.Title = trimmed;
            }

            return i + 1; // return index of next line after title
        }

        return 0; // no title found, start from beginning
    }

    // ──────────────────────────────────────────────
    //  Format Detection
    // ──────────────────────────────────────────────

    public SongFormat DetectFormat(string[] lines, int startIndex)
    {
        int inlineScore = 0;
        int alternatingScore = 0;
        int linesChecked = 0;

        for (int i = startIndex; i < lines.Length && linesChecked < 30; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            linesChecked++;

            // Check for inline bracket chords: [F], [Dm], [C/E]
            if (InlineBracketChordRegex.IsMatch(line))
            {
                inlineScore += 3;
            }

            // Check for alternating: a pure chord line followed by a lyric line
            if (IsChordLineFromString(line))
            {
                int nextNonEmpty = FindNextNonEmptyLine(lines, i + 1);
                if (nextNonEmpty >= 0 && !IsChordLineFromString(lines[nextNonEmpty]))
                {
                    alternatingScore += 3;
                }
            }
        }

        if (inlineScore > alternatingScore && inlineScore > 0)
            return SongFormat.InlineChord;
        if (alternatingScore > 0)
            return SongFormat.AlternatingChordLyric;
        return SongFormat.Unknown;
    }

    // ──────────────────────────────────────────────
    //  Inline Chord Parser  ([F]word [Dm]word ...)
    // ──────────────────────────────────────────────

    private void ParseInlineFormat(
        string[] lines, int startIndex,
        ICollection<SegmentCreateDto> songLyrics,
        ref int lineNumber, ref int lyricOrder)
    {
        for (int i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Split the line into segments: text before first bracket (if any) + [chord]text pairs
            var segments = ParseInlineLine(line);

            foreach (var (chord, lyric) in segments)
            {
                if (string.IsNullOrEmpty(lyric) && string.IsNullOrEmpty(chord))
                    continue;

                songLyrics.Add(new SegmentCreateDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Lyric = lyric ?? string.Empty,
                    LineNumber = lineNumber,
                    ChordId = chord != null ? Guid.NewGuid().ToString() : null,
                    ChordName = chord,
                    PartNumber = 0,
                    PartName = SongSection.unknown,
                    LyricOrder = lyricOrder++,
                    ChordAlignment = Alignment.Left
                });
            }

            lineNumber++;
        }
    }

    private List<(string? chord, string lyric)> ParseInlineLine(string line)
    {
        var result = new List<(string? chord, string lyric)>();

        // Find all [chord]lyric pairs
        var matches = InlineChordSegmentRegex.Matches(line);
        if (matches.Count == 0)
        {
            // No brackets at all — treat entire line as lyric-only, split by words
            var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var w in words)
            {
                result.Add((null, w));
            }
            return result;
        }

        // Leading text before first bracket
        int firstBracket = line.IndexOf('[');
        if (firstBracket > 0)
        {
            var leading = line.Substring(0, firstBracket).Trim();
            if (!string.IsNullOrEmpty(leading))
            {
                // Split leading text into words
                foreach (var w in leading.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    result.Add((null, w));
                }
            }
        }

        foreach (Match m in matches)
        {
            var chordRaw = m.Groups[1].Value.Trim();
            var lyricRaw = m.Groups[2].Value;

            // Validate chord; if invalid, treat entire match as lyric
            string? chord = IsChordWord(chordRaw) ? chordRaw : null;

            // Split lyric part into words to create individual segments
            var words = lyricRaw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                // Chord with no following text
                result.Add((chord, string.Empty));
            }
            else
            {
                // First word gets the chord
                result.Add((chord, words[0]));
                // Remaining words get no chord
                for (int w = 1; w < words.Length; w++)
                {
                    result.Add((null, words[w]));
                }
            }
        }

        return result;
    }

    // ──────────────────────────────────────────────
    //  Alternating Chord/Lyric Parser
    // ──────────────────────────────────────────────

    private void ParseAlternatingFormat(
        string[] lines, int startIndex,
        ICollection<SegmentCreateDto> songLyrics,
        ref int lineNumber, ref int lyricOrder)
    {
        for (int i = startIndex; i < lines.Length;)
        {
            var line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            if (IsChordLineFromString(line))
            {
                // Look for the next lyric line
                int nextLyric = FindNextNonEmptyLine(lines, i + 1);
                if (nextLyric >= 0 && !IsChordLineFromString(lines[nextLyric]))
                {
                    AlignChordsToLyrics(songLyrics, line, lines[nextLyric], lineNumber, ref lyricOrder);
                    lineNumber++;
                    i = nextLyric + 1;
                }
                else
                {
                    // Chord-only line with no matching lyric — add chords with empty lyrics
                    var chords = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Where(IsChordWord).ToArray();
                    foreach (var c in chords)
                    {
                        songLyrics.Add(CreateSegment(c, string.Empty, lineNumber, ref lyricOrder));
                    }
                    lineNumber++;
                    i++;
                }
            }
            else
            {
                // Lyric line without preceding chord line
                AddLyricsOnlyFromText(songLyrics, line, lineNumber, ref lyricOrder);
                lineNumber++;
                i++;
            }
        }
    }

    private void AlignChordsToLyrics(
        ICollection<SegmentCreateDto> songLyrics,
        string chordLine, string lyricLine,
        int lineNumber, ref int lyricOrder)
    {
        // Extract chords with character positions
        var chordPositions = new List<(string chord, int position)>();
        var chordMatches = ChordPattern.Matches(chordLine);
        foreach (Match m in chordMatches)
        {
            if (m.Length <= MAX_CHORD_LENGTH)
                chordPositions.Add((m.Value, m.Index));
        }

        if (chordPositions.Count == 0)
        {
            AddLyricsOnlyFromText(songLyrics, lyricLine, lineNumber, ref lyricOrder);
            return;
        }

        // Tokenize lyric line into syllables (splitting on spaces and hyphens)
        var lyricTokens = TokenizeLyricLine(lyricLine);

        if (lyricTokens.Count == 0)
        {
            // No lyrics, just add chords with empty lyrics
            foreach (var (chord, _) in chordPositions)
            {
                songLyrics.Add(CreateSegment(chord, string.Empty, lineNumber, ref lyricOrder));
            }
            return;
        }

        // If count of chords matches tokens, do 1:1 mapping
        if (chordPositions.Count == lyricTokens.Count)
        {
            for (int j = 0; j < lyricTokens.Count; j++)
            {
                songLyrics.Add(CreateSegment(chordPositions[j].chord, lyricTokens[j], lineNumber, ref lyricOrder));
            }
            return;
        }

        // Otherwise, use position-based alignment:
        // Get lyric words with their character positions from the original line
        var wordPositions = new List<(string word, int position)>();
        var wordRegex = new Regex(@"\S+");
        foreach (Match wm in wordRegex.Matches(lyricLine))
        {
            wordPositions.Add((wm.Value, wm.Index));
        }

        foreach (var (word, wpos) in wordPositions)
        {
            // Find the closest chord by character position
            string? matchedChord = null;
            if (chordPositions.Count > 0)
            {
                var closest = chordPositions.OrderBy(c => Math.Abs(c.position - wpos)).First();
                if (Math.Abs(closest.position - wpos) <= 5)
                {
                    matchedChord = closest.chord;
                }
            }

            songLyrics.Add(CreateSegment(matchedChord, word, lineNumber, ref lyricOrder));
        }
    }

    private List<string> TokenizeLyricLine(string lyricLine)
    {
        var tokens = new List<string>();
        // Split by spaces first
        var parts = lyricLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            // If part contains hyphens (syllable separators), split further
            if (part.Contains('-'))
            {
                var syllables = part.Split('-', StringSplitOptions.RemoveEmptyEntries);
                foreach (var syl in syllables)
                {
                    var trimmed = syl.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        tokens.Add(trimmed);
                }
            }
            else
            {
                tokens.Add(part);
            }
        }
        return tokens;
    }

    // ──────────────────────────────────────────────
    //  Plain Lyrics Parser (no chords detected)
    // ──────────────────────────────────────────────

    private void ParsePlainLyrics(
        string[] lines, int startIndex,
        ICollection<SegmentCreateDto> songLyrics,
        ref int lineNumber, ref int lyricOrder)
    {
        for (int i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            AddLyricsOnlyFromText(songLyrics, line, lineNumber, ref lyricOrder);
            lineNumber++;
        }
    }

    // ──────────────────────────────────────────────
    //  Shared Helpers
    // ──────────────────────────────────────────────

    private bool IsChordWord(string text)
    {
        return ChordRegex.IsMatch(text) && text.Length <= MAX_CHORD_LENGTH;
    }

    private bool IsChordLineFromString(string line)
    {
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 0 && words.All(IsChordWord);
    }

    private int FindNextNonEmptyLine(string[] lines, int from)
    {
        for (int i = from; i < lines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
                return i;
        }
        return -1;
    }

    private SegmentCreateDto CreateSegment(string? chord, string lyric, int lineNumber, ref int lyricOrder)
    {
        return new SegmentCreateDto
        {
            Id = Guid.NewGuid().ToString(),
            Lyric = lyric,
            LineNumber = lineNumber,
            ChordId = chord != null ? Guid.NewGuid().ToString() : null,
            ChordName = chord,
            PartNumber = 0,
            PartName = SongSection.unknown,
            LyricOrder = lyricOrder++,
            ChordAlignment = Alignment.Left
        };
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
            string? chord = null;

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
                songLyrics.Add(CreateSegment(chord, lyric, lineNumber, ref lyricOrder));
            }
        }
    }

    // ──────────────────────────────────────────────
    //  PDF-specific Helpers
    // ──────────────────────────────────────────────

    private List<List<Word>> GroupWordsIntoLines(List<Word> words)
    {
        var lines = new List<List<Word>>();
        if (words.Count == 0) return lines;

        var currentLine = new List<Word> { words[0] };
        double prevY = (words[0].BoundingBox.Top + words[0].BoundingBox.Bottom) / 2;

        for (int i = 1; i < words.Count; i++)
        {
            double currentY = (words[i].BoundingBox.Top + words[i].BoundingBox.Bottom) / 2;
            if (Math.Abs(currentY - prevY) > 10)
            {
                lines.Add(currentLine.OrderBy(w => w.BoundingBox.Left).ToList());
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

    private bool IsChordLinePdf(List<Word> line)
    {
        return line.Count > 0 && line.All(w => IsChordWord(w.Text));
    }

    private void AddPairedSegmentsPdf(
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

            string? chord = null;
            if (match != null && Math.Abs(match.BoundingBox.Left - lyric.BoundingBox.Left) < 20)
            {
                chord = match.Text;
            }

            songLyrics.Add(CreateSegment(chord, lyric.Text, lineNumber, ref lyricOrder));
        }
    }

    // ──────────────────────────────────────────────
    //  OCR Text Extraction (tolerant of garbled text)
    // ──────────────────────────────────────────────

    public SimpleSongCreateDto ExtractFromOcrText(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
            return new SimpleSongCreateDto { Title = "Scanned Song", SongLyrics = new List<SegmentCreateDto>() };

        var cleaned = CleanOcrText(ocrText);
        var song = ExtractFromRawText(cleaned);

        if (string.IsNullOrEmpty(song.Title) || song.Title == "Untitled Song")
            song.Title = "Scanned Song";

        return song;
    }

    private string CleanOcrText(string ocrText)
    {
        var lines = ocrText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var cleaned = new List<string>();

        foreach (var line in lines)
        {
            var l = line;

            // Remove common OCR junk characters
            l = l.Replace("\u00bb", "").Replace("\u00ab", ""); // » «
            l = l.Replace("\u2013", "-").Replace("\u2014", "-"); // – —
            l = l.Replace("\u2018", "'").Replace("\u2019", "'"); // ' '
            l = l.Replace("\u201c", "\"").Replace("\u201d", "\""); // " "
            l = Regex.Replace(l, @"[=]{2,}", ""); // == or ===
            l = l.Replace("=", " ");

            // Fix common chord OCR misreads: Xrn → Xm, 8m → Bm
            l = Regex.Replace(l, @"\b([A-G])rn\b", "$1m"); // Arn→Am, Crn→Cm, etc.
            l = Regex.Replace(l, @"\b8m\b", "Bm", RegexOptions.IgnoreCase);
            l = Regex.Replace(l, @"\b8b\b", "Bb", RegexOptions.IgnoreCase);
            l = Regex.Replace(l, @"\b([A-G])rnaj\b", "$1maj"); // Arnaj→Amaj
            l = Regex.Replace(l, @"\b([A-G])irn\b", "$1im"); // misread dim
            l = Regex.Replace(l, @"\b([A-G])rn(\d)\b", "$1m$2"); // Arn7→Am7
            l = l.Replace("\u00e9", "6"); // Gmé→Gm6 (OCR misreads 6 as é)
            l = l.Replace("\u00a9", ""); // © copyright symbol artifact

            // Remove stray ] [ that aren't part of inline chord [X] notation
            if (!InlineBracketChordRegex.IsMatch(l))
            {
                l = l.Replace("[", "").Replace("]", "");
            }

            // Collapse multiple spaces
            l = Regex.Replace(l, @"\s{2,}", " ").Trim();

            // Strip metadata preamble lines (not part of the song)
            if (Regex.IsMatch(l, @"(?i)^hymn\s*no\b") ||
                Regex.IsMatch(l, @"(?i)^category\s*:") ||
                Regex.IsMatch(l, @"(?i)^key\s*:") ||
                Regex.IsMatch(l, @"(?i)^tempo\s*:") ||
                Regex.IsMatch(l, @"(?i)^capo\s*:") ||
                Regex.IsMatch(l, @"(?i)^time\s*signature\s*:") ||
                Regex.IsMatch(l, @"^\d+/\d+\s*time\b", RegexOptions.IgnoreCase))
            {
                cleaned.Add("");
                continue;
            }

            cleaned.Add(l);
        }

        // Remove excessive consecutive blank lines (keep max 1)
        var result = new List<string>();
        int consecutiveBlanks = 0;
        foreach (var line in cleaned)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                consecutiveBlanks++;
                if (consecutiveBlanks <= 1)
                    result.Add(line);
            }
            else
            {
                consecutiveBlanks = 0;
                result.Add(line);
            }
        }

        return string.Join("\n", result);
    }

    // ──────────────────────────────────────────────
    //  DOCX Extraction
    // ──────────────────────────────────────────────

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

