using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Dtos;

namespace FRELODYSHRD.Dtos
{
    public class ChorusDto : BaseEntityDto
    {
        public string SongId { get; set; }

        [Range(0, 12)]
        public int? ChorusNumber { get; set; }

        public string? ChorusTitle { get; set; }

        public virtual ICollection<LyricLineDto>? LyricLines { get; set; }
    }
}
