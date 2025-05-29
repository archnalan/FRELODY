using FRELODYAPP.Interfaces;
using FRELODYSHRD.Dtos.EditDtos;
using System.ComponentModel.DataAnnotations;

namespace FRELODYSHRD.Dtos.HybridDtos
{
    public class ChordWithChartsDto
    {
        public string? Id { get; set; }
        public string ChordName { get; set; }

        [Range(1, 3)]
        public ChordDifficulty? Difficulty { get; set; }
        public string? ChordAudioFilePath { get; set; }
        public List<ChordChartEditDto>? Charts { get; set; }
    }
}
