using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.CreateDtos
{


    public class ChordChartForm
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please select a chord")]
        public string ChordId { get; set; }

        [Required]
        [Range(1, 24, ErrorMessage = "Fret position must be between 1 and 24")]
        public int FretPosition { get; set; } = 1;

        [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters")]
        public string? PositionDescription { get; set; }

        public string? FilePath { get; set; }
        public string? ChartAudioFilePath { get; set; }
    }
}
