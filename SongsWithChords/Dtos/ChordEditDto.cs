using SongsWithChords.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace SongsWithChords.Dtos
{
	public class ChordEditDto
	{
        public long Id { get; set; }
        public string ChordName { get; set; }
		public string? ChordAudioFilePath { get; set; }

		public ChordType? ChordType { get; set; }

        [Range(1, 3)]
		public ChordDifficulty? Difficulty { get; set; }			
	}
}
