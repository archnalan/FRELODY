using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos
{
    public class ChordDto : BaseEntityDto
    {
        [Key]
        public long Id { get; set; }

        [StringLength(15)]
        [RegularExpression(@"^([A-G])(#|b|bb|##)?(m|maj|min|sus|aug|dim|add)?(\d+)?(/([A-G])(#|b|bb|##)?)?$",
        ErrorMessage = "Invalid Chord Format!")]
        public string ChordName { get; set; }

        [Range(1, 3)]
        public ChordDifficulty? Difficulty { get; set; }
        public ChordType? ChordType { get; set; }

        //Will be assigned path of guitar chord position 1
        [StringLength(255)]
        public string? ChordAudioFilePath { get; set; }


        public virtual ICollection<ChordChartDto>? ChordCharts { get; set; }

        public virtual ICollection<LyricSegmentDto>? LyricSegments { get; set; }

        public ChordDto()
        {
            ChordCharts = new HashSet<ChordChartDto>();
            LyricSegments = new HashSet<LyricSegmentDto>();
        }
    }
}
