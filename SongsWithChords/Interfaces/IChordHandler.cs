namespace SongsWithChords.Interfaces
{
	public interface IChordHandler
	{
		public void StoreOriginalChords(string[] chords);
		public string[] ResetChords();
		public string[] TransposeChords(int semitones);		
		public string TransposeChord(string chord, int semitones);
	}
	
}
