using Microsoft.AspNetCore.Http;
using FRELODYAPP.Data;
using FRELODYAPP.Models.SubModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
	public class LyricSegment:BaseEntity
	{
		[Key]
        public Guid Id { get; set; }

        [Required]
		[StringLength(200)]
        public string Lyric { get; set; }		
		
		public long LyricOrder { get; set; }
		public int LineNumber { get; set; }

        [NotMapped]
		[TextFileValidation(".txt", ".pdf")]
		public IFormFile? LyricUpload { get; set; }

		[StringLength(255)]
		public string? LyricFilePath { get; set; }
		
		public long? ChordId { get; set; }

		public Guid? LyricLineId{ get;  set; }	

		[ForeignKey(nameof(ChordId))]
		public virtual Chord? Chord { get; set; }
	}
}
