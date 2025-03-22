using FRELODYAPP.Dtos;
using FRELODYSHRD.ModelTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class LineCreateDto
    {
        public long LyricLineOrder { get; set; }
        public SongSection PartName { get; set; }//Verse or bridge or chorus
        public int? PartNumber { get; set; }// verse or bridge number: chorus number can be null
        public int? RepeatCount { get; set; }
        public Guid? VerseId { get; set; }
        public Guid? ChorusId { get; set; }
        public Guid? BridgeId { get; set; }
        public ICollection<LyricSegmentCreateDto>? LyricSegments { get; set; }

    }
}
