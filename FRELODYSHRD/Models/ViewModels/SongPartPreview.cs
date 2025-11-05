using FRELODYSHRD.Dtos.CreateDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Models.ViewModels
{
    public class SongPartPreview
    {
        public int Index { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public int SectionNumber { get; set; }
        public List<SegmentCreateDto> Segments { get; set; } = new();
    }
}
