using FRELODYAPP.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYAPP.Interfaces;
using Microsoft.AspNetCore.Http;
using FRELODYSHRD.Dtos;

namespace FRELODYAPP.Dtos
{
    public class SongDto : BaseEntityDto
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [Display(Name = "SDAH-")]
        public int? SongNumber { get; set; }

        [Required]
        [StringLength(200)]
        public string? Slug { get; set; }

        public PlayLevel? SongPlayLevel { get; set; }

        [NotMapped]
        [TextFileValidation(".txt", ".pdf")]
        public IFormFile? TextUpload { get; set; }

        [StringLength(255)]
        public string? TextFilePath { get; set; }

        [StringLength(100)]
        public string? WrittenDateRange { get; set; }

        [StringLength(100)]
        public string? WrittenBy { get; set; }

        [StringLength(255)]
        public string? History { get; set; }

        [StringLength(200)]
        public string? AddedBy { get; set; }

        public string? CategoryId { get; set; }
        public string? AlbumId { get; set; }

        public bool? IsFavorite { get; set; }

        [Range(0, 5)]
        [Column(TypeName = "decimal(3,2)")]
        public decimal? Rating { get; set; }

        public virtual ICollection<SongPartDto>? SongParts { get; set; }
        public virtual ICollection<UserFeedbackDto>? Feedback { get; set; }        
    }
}
