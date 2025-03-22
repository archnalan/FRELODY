using FRELODYAPP.Dtos;
using FRELODYLIB.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class ChorusCreateDto : ISongPartDto
    {
        public Guid? SongId { get; set; }

        [Range(0, 12)]
        public int ChorusNumber { get; set; }

        public string? ChorusTitle { get; set; }

        public int? RepeatCount { get; set; }

        public virtual ICollection<LineCreateDto>? LyricLines { get; set; }

        public int GetPartNumber()
        {
            return ChorusNumber;
        }
    }
}
