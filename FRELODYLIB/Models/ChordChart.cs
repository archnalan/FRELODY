using FRELODYAPP.Data;
using FRELODYAPP.Models.SubModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
	public class ChordChart:BaseEntity
	{
        [Required]
        public string FilePath { get; set; }
        public string? ChordId { get; set; }

        [Range(1,24)]
        public int? FretPosition { get; set; }

		[StringLength(255)]
		public string? ChartAudioFilePath { get; set; }

		[StringLength(100)]
        public string? PositionDescription { get; set; }
    }
}
