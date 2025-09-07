using FRELODYAPP.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FRELODYSHRD.Services
{
    public static class ChordTypeService
    {
        private static readonly Dictionary<string, ChordType> ChordQualityMappings = new Dictionary<string, ChordType>(StringComparer.OrdinalIgnoreCase)
        {
            // Major chords (default when no quality specified)
            { "", ChordType.Major },
            { "maj", ChordType.Major },
            { "M", ChordType.Major },
            { "major", ChordType.Major },
            { "Δ", ChordType.Major },
            
            // Minor chords
            { "m", ChordType.Minor },
            { "min", ChordType.Minor },
            { "-", ChordType.Minor },
            { "minor", ChordType.Minor },
            
            // Suspended chords
            { "sus", ChordType.Suspended },
            { "sus2", ChordType.Suspended },
            { "sus4", ChordType.Suspended },
            
            // Augmented chords
            { "aug", ChordType.Augmented },
            { "+", ChordType.Augmented },
            { "augmented", ChordType.Augmented },
            
            // Diminished chords
            { "dim", ChordType.Diminished },
            { "o", ChordType.Diminished },
            { "°", ChordType.Diminished },
            { "diminished", ChordType.Diminished },
            
            // Sixth chords
            { "6", ChordType.Sixth },
            { "m6", ChordType.MinorSixth },
            { "-6", ChordType.MinorSixth },
            
            // Basic seventh chords
            { "7", ChordType.DominantSeventh },
            { "dom7", ChordType.DominantSeventh },
            { "maj7", ChordType.MajorSeventh },
            { "M7", ChordType.MajorSeventh },
            { "Δ7", ChordType.MajorSeventh },
            { "m7", ChordType.MinorSeventh },
            { "-7", ChordType.MinorSeventh },
            { "dim7", ChordType.DiminishedSeventh },
            { "o7", ChordType.DiminishedSeventh },
            { "°7", ChordType.DiminishedSeventh },
            { "m7b5", ChordType.HalfDiminishedSeventh },
            { "-7b5", ChordType.HalfDiminishedSeventh },
            { "ø7", ChordType.HalfDiminishedSeventh },
            { "ø", ChordType.HalfDiminishedSeventh },
            { "halfdim", ChordType.HalfDiminishedSeventh },
            { "7sus4", ChordType.SeventhSuspendedFourth },
            { "7sus", ChordType.SeventhSuspendedFourth },
            { "mm7", ChordType.MinorMajorSeventh },
            { "mM7", ChordType.MinorMajorSeventh },
            { "minM7", ChordType.MinorMajorSeventh },
            { "-M7", ChordType.MinorMajorSeventh },
            { "aug7", ChordType.AugmentedSeventh },
            { "+7", ChordType.AugmentedSeventh },
            
            // Extended seventh chords
            { "7#9", ChordType.SeventhSharpNine },
            { "7b9", ChordType.SeventhFlatNine },
            { "7#5", ChordType.SeventhSharpFive },
            { "7b5", ChordType.SeventhFlatFive },
            { "7b13", ChordType.SeventhFlatThirteen },
            { "7#11", ChordType.SeventhSharpEleven },
            { "7add9", ChordType.SeventhNinth },
            { "7(9)", ChordType.SeventhNinth },
            { "7add13", ChordType.SeventhThirteenth },
            { "7(13)", ChordType.SeventhThirteenth },
            
            // Ninth chords
            { "9", ChordType.Ninth },
            { "m9", ChordType.Ninth },
            { "maj9", ChordType.Ninth },
            { "M9", ChordType.Ninth },
            
            // Eleventh chords
            { "11", ChordType.AugmentedEleventh },
            { "m11", ChordType.AugmentedEleventh },
            { "maj11", ChordType.AugmentedEleventh },
            { "M11", ChordType.AugmentedEleventh },
            { "aug11", ChordType.AugmentedEleventh },
            { "+11", ChordType.AugmentedEleventh },
            
            // Thirteenth chords
            { "13", ChordType.Thirteenth },
            { "m13", ChordType.Thirteenth },
            { "maj13", ChordType.Thirteenth },
            { "M13", ChordType.Thirteenth },
            
            // Complex thirteenth variations
            { "13b9", ChordType.ThirteenthFlatNine },
            { "13#9", ChordType.ThirteenthSharpNine },
            { "13b5", ChordType.ThirteenthFlatFive },
            { "13#11", ChordType.ThirteenthSharpEleven },
            { "13#5", ChordType.ThirteenthSharpFifth },
            { "13b9b5", ChordType.ThirteenthFlatNineFlatFive },
            { "13b9#5", ChordType.ThirteenthFlatNineSharpFive },
            { "13#9b5", ChordType.ThirteenthSharpNineFlatFive },
            { "13#9#5", ChordType.ThirteenthSharpNineSharpFive },
            { "13b9#11", ChordType.ThirteenthFlatNineSharpEleven },
            { "13#9#11", ChordType.ThirteenthSharpNineSharpEleven },
            { "13#9b13", ChordType.ThirteenthSharpNineFlatThirteen },
            { "13#9#13", ChordType.ThirteenthSharpNineSharpThirteen },
            { "13b9#13", ChordType.ThirteenthFlatNineSharpThirteen },
            { "13b9b13", ChordType.ThirteenthFlatNineFlatThirteen },
            { "13#5#9", ChordType.ThirteenthSharpFiveSharpNine },
            { "13#5b9", ChordType.ThirteenthSharpFiveFlatNine },
            { "13b5#9", ChordType.ThirteenthFlatFiveSharpNine }
        };

        /// <summary>
        /// Determines the ChordType from a given chord name string using a robust pattern matching approach.
        /// </summary>
        /// <param name="chordName">The chord name to parse (e.g., "Cmaj7", "Am", "G7")</param>
        /// <returns>The corresponding ChordType, or null if the chord cannot be parsed</returns>
        public static ChordType? DetermineChordType(string chordName)
        {
            if (string.IsNullOrWhiteSpace(chordName))
                return null;

            try
            {
                return DetermineChordTypeRobust(chordName);
            }
            catch
            {
                // Fallback to legacy method if robust parsing fails
                return DetermineChordTypeLegacy(chordName);
            }
        }

        /// <summary>
        /// Robust chord type determination using longest-match pattern approach.
        /// </summary>
        /// <param name="chordName">The chord name to parse</param>
        /// <returns>The corresponding ChordType</returns>
        private static ChordType DetermineChordTypeRobust(string chordName)
        {
            if (string.IsNullOrWhiteSpace(chordName))
            {
                return ChordType.Major; // Default fallback
            }

            // Extract quality/suffix after root note
            var rootPattern = new Regex(@"^([A-Ga-g][#♯b♭]?)([^/]*)(?:/([A-Ga-g][#♯b♭]?))?$", RegexOptions.IgnoreCase);
            var match = rootPattern.Match(chordName);
            string quality = match.Success ? match.Groups[2].Value.Trim() : chordName.Trim();

            // Normalize quality - handle various musical notation symbols
            quality = NormalizeChordQuality(quality);

            // Match from longest/most specific to shortest to avoid partial matches
            return MatchChordQualityByLength(quality);
        }

        /// <summary>
        /// Normalizes chord quality notation to a standard format.
        /// </summary>
        /// <param name="quality">Raw chord quality string</param>
        /// <returns>Normalized chord quality string</returns>
        private static string NormalizeChordQuality(string quality)
        {
            return quality.ToLowerInvariant()
                .Replace("♭", "b")       // Unicode flat symbol
                .Replace("♯", "#")       // Unicode sharp symbol
                .Replace("min", "m")     // Standardize minor notation
                .Replace("major", "M")   // Standardize major notation
                .Replace("delta", "Δ")   // Delta symbol for major 7
                .Replace("diminished", "dim")
                .Replace("augmented", "aug")
                .Replace("halfdim", "ø")
                .Replace(" ", "");       // Remove all spaces
        }

        /// <summary>
        /// Matches chord quality using longest-first pattern matching to ensure specificity.
        /// </summary>
        /// <param name="quality">Normalized chord quality string</param>
        /// <returns>Matching ChordType</returns>
        private static ChordType MatchChordQualityByLength(string quality)
        {
            // Complex thirteenth variations (longest patterns first)
            if (quality.EndsWith("13b9#5")) return ChordType.ThirteenthFlatNineSharpFive;
            if (quality.EndsWith("13#9b5")) return ChordType.ThirteenthSharpNineFlatFive;
            if (quality.EndsWith("13#9#5")) return ChordType.ThirteenthSharpNineSharpFive;
            if (quality.EndsWith("13b9b5")) return ChordType.ThirteenthFlatNineFlatFive;
            if (quality.EndsWith("13b9#11")) return ChordType.ThirteenthFlatNineSharpEleven;
            if (quality.EndsWith("13#9#11")) return ChordType.ThirteenthSharpNineSharpEleven;
            if (quality.EndsWith("13#9b13")) return ChordType.ThirteenthSharpNineFlatThirteen;
            if (quality.EndsWith("13#9#13")) return ChordType.ThirteenthSharpNineSharpThirteen;
            if (quality.EndsWith("13b9#13")) return ChordType.ThirteenthFlatNineSharpThirteen;
            if (quality.EndsWith("13b9b13")) return ChordType.ThirteenthFlatNineFlatThirteen;
            if (quality.EndsWith("13#5#9")) return ChordType.ThirteenthSharpFiveSharpNine;
            if (quality.EndsWith("13#5b9")) return ChordType.ThirteenthSharpFiveFlatNine;
            if (quality.EndsWith("13b5#9")) return ChordType.ThirteenthFlatFiveSharpNine;

            // Simpler thirteenth variations
            if (quality.EndsWith("13b9")) return ChordType.ThirteenthFlatNine;
            if (quality.EndsWith("13#9")) return ChordType.ThirteenthSharpNine;
            if (quality.EndsWith("13b5")) return ChordType.ThirteenthFlatFive;
            if (quality.EndsWith("13#11")) return ChordType.ThirteenthSharpEleven;
            if (quality.EndsWith("13#5")) return ChordType.ThirteenthSharpFifth;

            // Extended seventh chords
            if (quality.EndsWith("7add13") || quality.EndsWith("7(13)")) return ChordType.SeventhThirteenth;
            if (quality.EndsWith("7add9") || quality.EndsWith("7(9)")) return ChordType.SeventhNinth;
            if (quality.EndsWith("7#9")) return ChordType.SeventhSharpNine;
            if (quality.EndsWith("7b9")) return ChordType.SeventhFlatNine;
            if (quality.EndsWith("7#5")) return ChordType.SeventhSharpFive;
            if (quality.EndsWith("7b5")) return ChordType.SeventhFlatFive;
            if (quality.EndsWith("7b13")) return ChordType.SeventhFlatThirteen;
            if (quality.EndsWith("7#11")) return ChordType.SeventhSharpEleven;
            if (quality.EndsWith("7sus4") || quality.EndsWith("7sus")) return ChordType.SeventhSuspendedFourth;

            // Seventh chord variations (order matters - specific before general)
            if (quality.EndsWith("m7b5") || quality.EndsWith("-7b5")) return ChordType.HalfDiminishedSeventh;
            if (quality.EndsWith("ø7") || quality.EndsWith("ø")) return ChordType.HalfDiminishedSeventh;
            if (quality.EndsWith("dim7") || quality.EndsWith("o7") || quality.EndsWith("°7")) return ChordType.DiminishedSeventh;
            if (quality.EndsWith("aug7") || quality.EndsWith("+7")) return ChordType.AugmentedSeventh;
            if (quality.EndsWith("mm7") || quality.EndsWith("mM7") || quality.EndsWith("minM7") || quality.EndsWith("-M7")) return ChordType.MinorMajorSeventh;
            if (quality.EndsWith("m7") || quality.EndsWith("-7")) return ChordType.MinorSeventh;
            if (quality.EndsWith("M7") || quality.EndsWith("maj7") || quality.EndsWith("Δ7")) return ChordType.MajorSeventh;
            if (quality.EndsWith("dom7")) return ChordType.DominantSeventh;

            // Extended chords (11th, 13th, 9th)
            if (quality.EndsWith("aug11") || quality.EndsWith("+11")) return ChordType.AugmentedEleventh;
            if (quality.EndsWith("m6") || quality.EndsWith("-6")) return ChordType.MinorSixth;
            if (quality.EndsWith("6")) return ChordType.Sixth;
            if (quality.EndsWith("9")) return ChordType.Ninth;
            if (quality.EndsWith("11")) return ChordType.AugmentedEleventh;
            if (quality.EndsWith("13")) return ChordType.Thirteenth;

            // Basic seventh (must come after extended seventh checks)
            if (quality.EndsWith("7")) return ChordType.DominantSeventh;

            // Triads and basic qualities
            if (quality.EndsWith("dim") || quality.EndsWith("o") || quality.EndsWith("°")) return ChordType.Diminished;
            if (quality.EndsWith("aug") || quality.EndsWith("+")) return ChordType.Augmented;
            if (quality.EndsWith("sus2") || quality.EndsWith("sus4") || quality.EndsWith("sus")) return ChordType.Suspended;
            if (quality.EndsWith("m") || quality.EndsWith("-")) return ChordType.Minor;
            if (quality.EndsWith("M") || quality.EndsWith("maj") || quality.EndsWith("Δ") || string.IsNullOrEmpty(quality)) return ChordType.Major;

            // Default fallback
            return ChordType.Major;
        }

        /// <summary>
        /// Legacy chord type determination method using regex and dictionary lookup.
        /// Kept as fallback for compatibility.
        /// </summary>
        /// <param name="chordName">The chord name to parse</param>
        /// <returns>The corresponding ChordType, or null if the chord cannot be parsed</returns>
        private static ChordType? DetermineChordTypeLegacy(string chordName)
        {
            if (string.IsNullOrWhiteSpace(chordName))
                return null;

            // Clean the chord name
            var cleanChordName = chordName.Trim().Replace(" ", "");

            // Use regex to extract chord components
            var match = Regex.Match(
                cleanChordName,
                @"^([A-G])(#|b|##|bb)?(?<quality>m|min|maj|dim|aug|5|sus2|sus4|6|m6|7|maj7|m7|dim7|m7b5|7sus4|7#9|7b9|7#5|7b5|7b13|7#11|9|m9|maj9|11|m11|maj11|13|m13|maj13|add9|add11|add13|13b9|13#9|13b5|13#11|13#5|13b9b5|13b9#5|13#9b5|13#9#5|13b9#11|13#9#11|13#9b13|13#9#13|13b9#13|13b9b13|13#5#9|13#5b9|13b5#9)?(?:\/[A-G](#|b|##|bb)?)?$",
                RegexOptions.IgnoreCase
            );

            if (!match.Success)
                return null;

            // Extract the chord quality
            var quality = match.Groups["quality"].Value;

            // Look up the chord type in our mapping
            if (ChordQualityMappings.TryGetValue(quality, out var chordType))
            {
                return chordType;
            }

            // If no quality is specified, it's a major chord
            if (string.IsNullOrEmpty(quality))
            {
                return ChordType.Major;
            }

            // Return null if chord quality is not recognized
            return null;
        }

        /// <summary>
        /// Gets all supported chord quality patterns for validation purposes.
        /// </summary>
        /// <returns>Collection of supported chord quality strings</returns>
        public static IEnumerable<string> GetSupportedChordQualities()
        {
            return ChordQualityMappings.Keys.Where(k => !string.IsNullOrEmpty(k)).OrderBy(k => k);
        }

        /// <summary>
        /// Validates if a chord name follows a supported pattern.
        /// </summary>
        /// <param name="chordName">The chord name to validate</param>
        /// <returns>True if the chord name is valid and supported</returns>
        public static bool IsValidChordName(string chordName)
        {
            if (string.IsNullOrWhiteSpace(chordName))
                return false;

            var determinedType = DetermineChordType(chordName);
            return determinedType.HasValue;
        }

        public static ChordDifficulty DetermineChordDifficulty(string chordName)
        {
            var chordType = DetermineChordType(chordName);

            if (!chordType.HasValue)
                return ChordDifficulty.Medium; 

            switch (chordType.Value)
            {
                // Easy chords
                case ChordType.Major:
                case ChordType.Minor:
                case ChordType.Suspended:
                case ChordType.Sixth:
                case ChordType.DominantSeventh:
                    return ChordDifficulty.Easy;

                // Intermediate chords
                case ChordType.MajorSeventh:
                case ChordType.MinorSeventh:
                case ChordType.MinorSixth:
                case ChordType.Ninth:
                case ChordType.Augmented:
                case ChordType.Diminished:
                case ChordType.HalfDiminishedSeventh:
                case ChordType.SeventhSuspendedFourth:
                    return ChordDifficulty.Medium;

                // Advanced chords (altered, extended, or rare forms)
                case ChordType.AugmentedSeventh:
                case ChordType.DiminishedSeventh:
                case ChordType.MinorMajorSeventh:
                case ChordType.SeventhSharpNine:
                case ChordType.SeventhFlatNine:
                case ChordType.SeventhSharpFive:
                case ChordType.SeventhFlatFive:
                case ChordType.SeventhFlatThirteen:
                case ChordType.SeventhSharpEleven:
                case ChordType.AugmentedEleventh:
                case ChordType.Thirteenth:
                case ChordType.ThirteenthFlatNine:
                case ChordType.ThirteenthSharpNine:
                case ChordType.ThirteenthFlatFive:
                case ChordType.ThirteenthSharpEleven:
                case ChordType.ThirteenthSharpFifth:
                case ChordType.ThirteenthFlatNineFlatFive:
                case ChordType.ThirteenthFlatNineSharpFive:
                case ChordType.ThirteenthSharpNineFlatFive:
                case ChordType.ThirteenthSharpNineSharpFive:
                case ChordType.ThirteenthFlatNineSharpEleven:
                case ChordType.ThirteenthSharpNineSharpEleven:
                case ChordType.ThirteenthSharpNineFlatThirteen:
                case ChordType.ThirteenthSharpNineSharpThirteen:
                case ChordType.ThirteenthFlatNineSharpThirteen:
                case ChordType.ThirteenthFlatNineFlatThirteen:
                case ChordType.ThirteenthSharpFiveSharpNine:
                case ChordType.ThirteenthSharpFiveFlatNine:
                case ChordType.ThirteenthFlatFiveSharpNine:
                    return ChordDifficulty.Advanced;

                // Fallback
                default:
                    return ChordDifficulty.Medium;
            }
        }

    }
}

