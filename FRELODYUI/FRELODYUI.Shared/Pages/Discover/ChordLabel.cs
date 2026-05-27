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
