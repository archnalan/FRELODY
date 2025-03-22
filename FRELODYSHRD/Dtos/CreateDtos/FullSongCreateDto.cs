using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class FullSongCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }
        public int? SongNumber { get; set; }
        public ICollection<VerseCreateDto>? Verses { get; set; }
        public ICollection<BridgeCreateDto>? Bridges { get; set; }
        public ICollection<ChorusCreateDto>? Choruses { get; set; }
    }
}
