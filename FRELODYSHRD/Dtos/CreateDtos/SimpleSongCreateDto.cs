using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class SimpleSongCreateDto
    {
        public string Title { get; set; }
        public int? SongNumber { get; set; }
        public ICollection<SegmentCreateDto>? SongLyrics { get; set; }
    }
}
