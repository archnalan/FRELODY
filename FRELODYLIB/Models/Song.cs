using FRELODYAPP.Data;
using FRELODYAPP.Interfaces;
using FRELODYAPP.Models.SubModels;
using FRELODYLIB.Models;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
	public class Song:BaseEntity
	{
		[Required]
		[StringLength(100)]
		public string Title { get; set; }

		public int? SongNumber { get; set; }

		[Required]
		[StringLength(200)]
		public string? Slug { get; set; }

        public PlayLevel? SongPlayLevel { get; set; }

        [NotMapped]
		[TextFileValidation(".txt", ".pdf")]
		public IFormFile? TextUpload { get; set; }
        
        public string? TextFileContent { get; set; }

        [StringLength(255)]
        public string? TextFilePath { get; set; }

        [StringLength(100)]
        public string? WrittenDateRange { get; set; }

		[StringLength(100)]
		public string? WrittenBy { get; set; }

		[StringLength(255)]
		public string? History { get; set; }
        public string? CategoryId { get; set; }
        public string? SongBookId { get; set; }
        public string? AlbumId { get; set; }
        public string? ArtistId { get; set; }

        [Range(0, 5)]
        [Column(TypeName = "decimal(3,2)")]
        public decimal? Rating { get; set; }

        public int Revision { get; set; } = 1;

        public virtual ICollection<SongPart>? SongParts { get; set; }

		public virtual ICollection<UserFeedback>? Feedback { get; set; }
    }
}
