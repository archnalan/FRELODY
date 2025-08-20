using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FRELODYAPP.Dtos;
using FRELODYAPP.Interfaces;

namespace FRELODYSHRD.Dtos
{
    public class ChordDto
    {
        public string? Id { get; set; }

        [StringLength(15)]
        [RegularExpression(@"^([A-G])(#|b|bb|##)?(m|maj|min|sus|aug|dim|add)?(\d+)?(/([A-G])(#|b|bb|##)?)?$",
        ErrorMessage = "Invalid Chord Format!")]
        public string ChordName { get; set; }

        [Range(1, 3)]
        public ChordDifficulty? Difficulty { get; set; }
        public ChordType? ChordType { get; set; }

        [StringLength(255)]
        public string? ChordAudioFilePath { get; set; }

        public virtual ICollection<ChordChartDto>? ChordCharts { get; set; }

    }
}
