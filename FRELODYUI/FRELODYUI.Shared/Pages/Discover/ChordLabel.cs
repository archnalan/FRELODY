using System.Collections.Generic;
using System.Linq;
using FRELODYUI.Shared.Services;

namespace FRELODYUI.Shared.Pages.Discover;

/// <summary>
/// Turns ChordMini's raw chord labels (e.g. "Bb:maj", "C:min7", "F#:maj/3", "N")
/// into clean musical notation for display ("B♭", "Cm7", "F♯", "—").
/// Display only — the raw label stays the source of truth in the DB/DTO.
/// </summary>
public static class ChordLabel
{
    /// <summary>"N"/"X"/blank are silence/unknown spans, not playable chords.</summary>
    public static bool IsNoChord(string? raw) =>
        string.IsNullOrWhiteSpace(raw) || raw is "N" or "X";

    /// <summary>
    /// Shifts a raw label's root (and note-bass) by <paramref name="semitones"/>,
    /// preserving the ":quality" and "/bass" structure. No-chord spans pass through
    /// untouched so rests keep their place in the grid. Used for capo: pass the
    /// negative capo fret count so the displayed chords are the shapes the guitarist
    /// actually fingers above the capo.
    /// </summary>
    public static string? TransposeRaw(string? raw, int semitones, bool preferFlat)
    {
        if (IsNoChord(raw) || semitones == 0) return raw;

        var s = raw!.Trim();

        // Split an optional "/bass" tail (may be a note like F# or a degree like 3).
        string main = s, bass = string.Empty;
        var slash = s.IndexOf('/');
        if (slash > 0) { main = s[..slash]; bass = s[(slash + 1)..]; }

        // Split "root:quality" — quality kept verbatim (with its leading ':').
        var colon = main.IndexOf(':');
        var root = colon >= 0 ? main[..colon] : main;
        var quality = colon >= 0 ? main[colon..] : string.Empty;

        var result = ChordTransposer.TransposeNote(root, semitones, preferFlat) + quality;

        if (slash > 0)
        {
            // Only transpose a lettered bass note; leave scale-degree basses (e.g. "3") alone.
            var tBass = bass.Length > 0 && char.IsLetter(bass[0])
                ? ChordTransposer.TransposeNote(bass, semitones, preferFlat)
                : bass;
            result += "/" + tBass;
        }
        return result;
    }

    /// <summary>
    /// ASCII chord name suitable for the chord-chart search API (e.g. "Bb:min7" → "Bbm7").
    /// Returns null for no-chord spans. Bass/inversion is dropped to match the catalog's
    /// root-position voicings.
    /// </summary>
    public static string? PlainName(string? raw)
    {
        if (IsNoChord(raw)) return null;
        var s = raw!.Trim();

        var slash = s.IndexOf('/');
        if (slash > 0) s = s[..slash];

        string root, quality;
        var colon = s.IndexOf(':');
        if (colon >= 0) { root = s[..colon]; quality = s[(colon + 1)..]; }
        else { root = s; quality = "maj"; }

        var suffix = quality switch
        {
            "maj" or "" => "",
            "min" => "m",
            "maj7" => "maj7",
            "min7" => "m7",
            "7" => "7",
            "6" or "maj6" => "6",
            "min6" => "m6",
            "dim" => "dim",
            "dim7" => "dim7",
            "hdim7" => "m7b5",
            "aug" => "aug",
            "sus2" => "sus2",
            "sus4" => "sus4",
            "9" => "9",
            "maj9" => "maj9",
            "min9" => "m9",
            _ => quality
        };
        return root + suffix;
    }

    /// <summary>
    /// Whether a set of raw labels prefers flat spelling (more 'b' than '#').
    /// Drives sharp/flat choice when transposing for a capo.
    /// </summary>
    public static bool PrefersFlat(IEnumerable<string?> raws)
    {
        int sharp = 0, flat = 0;
        foreach (var r in raws)
        {
            if (string.IsNullOrEmpty(r)) continue;
            foreach (var c in r) { if (c == '#') sharp++; else if (c == 'b') flat++; }
        }
        return flat > sharp;
    }

    public static string Display(string? raw)
    {
        if (IsNoChord(raw)) return "—";
        var s = raw!.Trim();

        // Drop bass/inversion (e.g. "C:maj/3") — noise for a play-along grid.
        var slash = s.IndexOf('/');
        if (slash > 0) s = s[..slash];

        string root, quality;
        var colon = s.IndexOf(':');
        if (colon >= 0) { root = s[..colon]; quality = s[(colon + 1)..]; }
        else { root = s; quality = "maj"; }

        var suffix = quality switch
        {
            "maj" or "" => "",
            "min" => "m",
            "maj7" => "maj7",
            "min7" => "m7",
            "7" => "7",
            "6" or "maj6" => "6",
            "min6" => "m6",
            "dim" => "dim",
            "dim7" => "dim7",
            "hdim7" => "m7♭5",
            "aug" => "aug",
            "sus2" => "sus2",
            "sus4" => "sus4",
            "9" => "9",
            "maj9" => "maj9",
            "min9" => "m9",
            _ => quality
        };
        return PrettyRoot(root) + suffix;
    }

    private static string PrettyRoot(string root)
    {
        if (string.IsNullOrEmpty(root)) return root;
        var letter = char.ToUpperInvariant(root[0]).ToString();
        var accidentals = root[1..].Replace("#", "♯").Replace("b", "♭");
        return letter + accidentals;
    }
}
