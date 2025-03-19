using SongsWithChords.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SongsWithChords.Models.SubModels;
using SongsWithChords.Models;
using SongsWithChords.Dtos.SubDtos;
using SongsWithChords.Interfaces;

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

        public virtual ICollection<Verse>? Verses { get; set; }
        public virtual ICollection<Bridge>? Bridges { get; set; }
        public virtual ICollection<Chorus>? Choruses { get; set; }
        public virtual ICollection<UserFeedback>? Feedback { get; set; }        
    }
}
