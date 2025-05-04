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
        public long Id { get; set; }

        [StringLength(15)]
        [RegularExpression(@"^([A-G])(#|b|##|bb)?(?:m|min|maj|dim|aug|5|sus2|sus4|6|m6|7|maj7|m7|dim7|m7b5|7sus4|7#9|7b9|7#5|7b5|7b13|7#11|9|m9|maj9|11|m11|maj11|13|m13|maj13|add9|add11|add13|13b9|13#9|13b5|13#11|13#5|13b9b5|13b9#5|13#9b5|13#9#5|13b9#11|13#9#11|13#9b13|13#9#13|13b9#13|13b9b13|13#5#9|13#5b9|13b5#9)?(?:\/[A-G](#|b|##|bb)?)?$", 
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
