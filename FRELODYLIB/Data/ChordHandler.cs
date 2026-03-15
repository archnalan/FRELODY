using FRELODYAPP.Interfaces;
using System.Text.RegularExpressions;

namespace FRELODYAPP.Data
{
	public class ChordHandler : IChordHandler
	{
		private string[] _originalChords = Array.Empty<string>();

		private static readonly Dictionary<string, int> SharpScale = new()
		{
			{ "C", 0 }, { "C#", 1 }, { "D", 2 }, { "D#", 3 }, { "E", 4 }, { "F", 5 },
			{ "F#", 6 }, { "G", 7 }, { "G#", 8 }, { "A", 9 }, { "A#", 10 }, { "B", 11 }
		};

		private static readonly Dictionary<string, int> FlatScale = new()
		{
			{ "C", 0 }, { "Db", 1 }, { "D", 2 }, { "Eb", 3 }, { "E", 4 }, { "F", 5 },
			{ "Gb", 6 }, { "G", 7 }, { "Ab", 8 }, { "A", 9 }, { "Bb", 10 }, { "B", 11 }
		};

		public string TransposeChord(string chord, int semitones)
		{
			chord = chord.Trim().Replace(" ", "");

			var match = Regex.Match(chord,
				@"^([A-G])(#|b)?(m|maj|min|sus|aug|dim|add)?(\d+)?(/([A-G])(#|b)?)?$");

			if (!match.Success) return chord;

			var rootNote = match.Groups[1].Value + match.Groups[2].Value;
			var chordQuality = match.Groups[3].Value + match.Groups[4].Value;
			var bassNote = !string.IsNullOrEmpty(match.Groups[5].Value)
				? match.Groups[6].Value + match.Groups[7].Value
				: string.Empty;

			var scale = DetermineScale(new[] { chord });

			var transposedRoot = TransposeNote(rootNote, semitones, scale);
			var transposedBass = !string.IsNullOrEmpty(bassNote)
				? TransposeNote(bassNote, semitones, scale)
				: string.Empty;

			return $"{transposedRoot}{chordQuality}" +
				   (string.IsNullOrEmpty(transposedBass) ? "" : "/" + transposedBass);
		}

		public string[] ResetChords()
		{
			return (string[])_originalChords.Clone();
		}

		public void StoreOriginalChords(string[] chords)
		{
			_originalChords = (string[])chords.Clone();
		}

		private static Dictionary<string, int> DetermineScale(IEnumerable<string> chords)
		{
			int sharpCount = chords.Sum(c => c.Count(ch => ch == '#'));
			int flatCount = chords.Sum(c => c.Count(ch => ch == 'b'));
			return flatCount > sharpCount ? FlatScale : SharpScale;
		}

		private static string TransposeNote(string note, int semitones, Dictionary<string, int> scale)
		{
			if (!scale.TryGetValue(note, out var noteIndex))
				return note;

			var newIndex = (noteIndex + semitones + 12) % 12;
			return scale.FirstOrDefault(x => x.Value == newIndex).Key ?? note;
		}
	}
}
