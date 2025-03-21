using FRELODYAPP.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FRELODYAPP.Data
{
	public class ChordTransposer:IChordHandler
	{
		private readonly Dictionary<string, int> _sharpChromaticScale;
		private readonly Dictionary<string, int> _flatChromaticScale;
		private  Dictionary<string, int> _usedScale;

		private List<string> _originalChords;
		public ChordTransposer()
		{
			_sharpChromaticScale = new Dictionary<string, int>
			{
				{ "C", 0},{"C#",1},{"D",2},{"D#",3}, {"E",4},{"F",5}, {"F#",6},
				{"G",7 },{"G#",8},{"A",9},{"A#",10},{"B",11}
			};
			_flatChromaticScale = new Dictionary<string, int>
			{
				{ "C", 0},{"Db",1},{"D",2},{"Eb",3}, {"E",4},{"F",5}, {"Gb",6},
				{"G",7 },{"Ab",8},{"A",9},{"Bb",10},{"B",11}
			};

			_originalChords = new List<string>();

		}

		public void StoreOriginalChords(string[] chords)
		{
			_originalChords = chords.ToList();
			_usedScale = DetermineScale(_originalChords);
		}

		public Dictionary<string, int> DetermineScale(List<string> chords)
		{
			int sharpCount = chords.Sum(chord=>chord.Count(c=>c=='#'));
			int flatCount = chords.Sum(chord => chord.Count(c => c == 'b'));

			if (flatCount > sharpCount) return _flatChromaticScale;

			if(sharpCount > flatCount) return _sharpChromaticScale;

			return _sharpChromaticScale;
		}

		public string[] ResetChords()
		{
			return _originalChords.ToArray();
		}

		public string[] TransposeChords(int semitones)
		{
			return _originalChords.Select(chord => TransposeChord(chord, semitones)).ToArray();
		}

		public string TransposeChord(string chord, int semitones)
		{			
			chord = chord.Trim().Replace(" ", "");//Remove any spaces

			var match = System.Text.RegularExpressions
				.Regex.Match(chord, @"^([A-G])(#|b)?(m|maj|min|sus|aug|dim|add)?(\d+)?(/([A-G])(#|b)?)?$");
			
			/*[RegularExpression(@"^([A-G])(b|#)?(m|maj|min|dim|aug|sus|add)?(2|4|5|6|7|9|11|13)?(#|b)?(\/[A-G](#|b)?)?$",
			ErrorMessage = "Invalid Chord Format!")]*/

			if (!match.Success) return chord;

			var rootNote = match.Groups[1].Value + match.Groups[2].Value;
			var chordQuality = match.Groups[3].Value + match.Groups[4];
			var bassNote = (string.IsNullOrEmpty(match.Groups[5].Value) == false) ? 
				match.Groups[5].Value.TrimStart('/'):string.Empty;

			//call the transposeNote method
			var transposedRoot = TransposeNote(rootNote, semitones);
			var transposedBass = !string.IsNullOrEmpty(bassNote)? 
				TransposeNote(bassNote, semitones): string.Empty;

			return $"{transposedRoot}{chordQuality}{(string.IsNullOrEmpty(transposedBass)?"":"/"+ transposedBass)}";
		}

		public string TransposeNote(string note, int semitones)
		{
			//Determine which scale was used
			var scale = _usedScale;

			try
			{
				//Get and manipulate the note in scale
				var noteIndex = scale[note];
				if (noteIndex == -1) return note;
				var newIndex = (noteIndex + semitones + 12) % 12;

				var newNote = scale.FirstOrDefault(x => x.Value == newIndex).Key;
				return newNote;

			}
			catch (Exception ex)
			{
				return $"Error: {ex.Message}";
			}
		}
	}
}
