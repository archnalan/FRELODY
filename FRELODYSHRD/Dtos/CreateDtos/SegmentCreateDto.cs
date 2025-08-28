using FRELODYAPP.Interfaces;
using FRELODYSHRD.ModelTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class SegmentCreateDto
    {
        public string? Id { get; set; } // key value for UI
        [Required]
        public string Lyric { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string? ChordId { get; set; }
        public string? ChordName { get; set; }
        [Required]
        public int PartNumber { get; set; } // SongSection value number
        [Required]
        public SongSection PartName { get; set; }
        [Required]
        public int LyricOrder { get; set; } 
        public bool AddNextSegment { get; set; } = false; // for UI purpose
        public Alignment? ChordAlignment { get; set; } = Alignment.Left;
    }
}
