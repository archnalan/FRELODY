using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Dtos
{
	public class VerseCreateDto
	{
        [Range(0, 24)]
        public int VerseNumber { get; set; }

        public Guid SongId { get; set; }
	}
}
