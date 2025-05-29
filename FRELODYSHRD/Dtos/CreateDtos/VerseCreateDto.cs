using System.ComponentModel.DataAnnotations;
using FRELODYLIB.Interfaces;

namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class VerseCreateDto : ISongPartDto
    {
        [Range(0, 24)]
        public int VerseNumber { get; set; }

        [MaxLength(100)]
        public string? VerseTitle { get; set; }

        public string? SongId { get; set; }

        public int? RepeatCount { get; set; }

        public ICollection<LineCreateDto>? LyricLines { get; set; }

        public int GetPartNumber()
        {
            return VerseNumber;
        }
    }
}
