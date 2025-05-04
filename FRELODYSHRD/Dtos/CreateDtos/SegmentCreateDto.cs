using FRELODYSHRD.ModelTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class SegmentCreateDto
    {
        public int? Id { get; set; } // key value for UI
        public string Lyric { get; set; }
        public int LineNumber { get; set; }
        public long? ChordId { get; set; }
        public int PartNumber { get; set; } // SongSection value number
        public SongSection PartName { get; set; }
        public int LyricOrder { get; set; } 
    }
}
