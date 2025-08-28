using Microsoft.AspNetCore.Http;
using FRELODYAPP.Data;
using FRELODYAPP.Models.SubModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FRELODYAPP.Interfaces;

namespace FRELODYAPP.Models
{
	public class LyricSegment:BaseEntity
	{
        [Required]
		[StringLength(200)]
        public string Lyric { get; set; }		
		
		public int LyricOrder { get; set; }

		public int LineNumber { get; set; }

        [NotMapped]
		[TextFileValidation(".txt", ".pdf")]
		public IFormFile? LyricUpload { get; set; }

        public string? LyricFileContent { get; set; }

        [StringLength(255)]
		public string? LyricFilePath { get; set; }

		public string? LyricLineId{ get;  set; }	
		
		public string? ChordId { get; set; }

		public Alignment? ChordAlignment { get; set; }

		[ForeignKey(nameof(ChordId))]
		public virtual Chord? Chord { get; set; }
	}
}
