using FRELODYSHRD.Dtos.EditDtos;
using FRELODYSHRD.Models.ChordDraw;
using System.ComponentModel.DataAnnotations;

namespace FRELODYSHRD.Dtos.HybridDtos
{
    public class ChartWithParentChordDto
    {
        public Guid Id { get; set; }
        public string? FilePath { get; set; }

        [Range(1, 24)]
        public int? FretPosition { get; set; }

        [StringLength(255)]
        public string? ChartAudioFilePath { get; set; }

        [StringLength(100)]
        public string? PositionDescription { get; set; }

        public ChordSource Source { get; set; } = ChordSource.Image;

        public ChordDrawData? ChordData { get; set; }

        public string? RenderedSvg { get; set; }

        public ChordEditDto? ParentChord { get; set; }
    }
}
