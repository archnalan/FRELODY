using System.ComponentModel.DataAnnotations;

namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class ChordChartCreateDto
    {
        public string FilePath { get; set; }
        public string? ChordId { get; set; }

        [Range(1, 24)]
        public int FretPosition { get; set; }

        [StringLength(100)]
        public string? PositionDescription { get; set; }
        
        [StringLength(255)]
        public string? ChartAudioFilePath { get; set; }
    }
}
