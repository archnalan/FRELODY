using FRELODYSHRD.ModelTypes;
using FRELODYUI.Shared.Models;
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
        public bool? IsRecoveryCopy { get; set; }
        public SongRecoveryDto? RecoveryDto { get; set; }
        public BookCategoryPairViewModel? BookCategory { get; set; }
        public ArtistAlbumPairViewModel? ArtistAlbum { get; set; }
        public Dictionary<SongSection, int>? PartRepeatCounts { get; set; } // section key, repeat count value
        public Dictionary<int, int>? LineRepeatCounts { get; set; } //line number key, repeat count value
        public ICollection<SegmentCreateDto>? SongLyrics { get; set; }
    }
}
