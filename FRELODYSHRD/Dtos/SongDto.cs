using SongsWithChords.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SongsWithChords.Dtos.SubDtos;
using SongsWithChords.Interfaces;
using Microsoft.AspNetCore.Http;
using FRELODYSHRD.Dtos;

namespace SongsWithChords.Dtos
{
    public class SongDto : BaseEntityDto
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [Display(Name = "SDAH-")]
        public long? SongNumber { get; set; }

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

        public Guid? CategoryId { get; set; }

        public virtual ICollection<VerseDto>? Verses { get; set; }
        public virtual ICollection<BridgeDto>? Bridges { get; set; }
        public virtual ICollection<ChorusDto>? Choruses { get; set; }
        public virtual ICollection<UserFeedbackDto>? Feedback { get; set; }        
    }
}
