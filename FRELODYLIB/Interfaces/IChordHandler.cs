namespace FRELODYAPP.Interfaces
{
	public interface IChordHandler
	{
		string TransposeChord(string chord, int semitones);
		string[] ResetChords();
		void StoreOriginalChords(string[] chords);
	}
}
