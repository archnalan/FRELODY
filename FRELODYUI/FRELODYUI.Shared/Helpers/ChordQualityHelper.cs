namespace FRELODYUI.Shared.Helpers
{
    /// <summary>
    /// Derives a short, user-facing label for a chord's quality from its name.
    /// Examples: "A" -> "Major", "Am" -> "Minor", "A/Ab" -> "Major" (slash is
    /// already visible in the name), "Am/B" -> "Minor", "Cmaj7" -> "maj7",
    /// "Cm7b5" -> "m7b5". Used by ChordCard for the badge and by ChordList
    /// for search matching.
    /// </summary>
    public static class ChordQualityHelper
    {
        public static string GetQualityLabel(string? chordName)
        {
            if (string.IsNullOrEmpty(chordName)) return string.Empty;

            // Strip root letter (1 char) + optional accidental (#, b, bb, ##).
            var i = 1;
            if (i < chordName.Length && (chordName[i] == '#' || chordName[i] == 'b'))
            {
                i++;
                if (i < chordName.Length && (chordName[i] == '#' || chordName[i] == 'b'))
                    i++;
            }

            if (i >= chordName.Length) return "Major";

            var suffix = chordName[i..];

            // Slash chord: peel the bass note and report the underlying chord's quality.
            // The "/X" is already visible in the displayed name, so the badge adds
            // value by telling the user what kind of chord it is, not that it's a slash.
            var slashIdx = suffix.IndexOf('/');
            if (slashIdx >= 0)
                suffix = suffix[..slashIdx];

            return suffix switch
            {
                "" => "Major",
                "m" => "Minor",
                _ => suffix
            };
        }
    }
}
