using Microsoft.AspNetCore.Http;
using SongsWithChords.Data;
using SongsWithChords.Models.SubModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SongsWithChords.Models
{
	public class LyricSegment:BaseEntity
	{
		[Key]
        public Guid Id { get; set; }

        [Required]
		[StringLength(200)]
        public string Lyric { get; set; }		
		
		public long LyricOrder { get; set; }

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
