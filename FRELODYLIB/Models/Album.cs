using FRELODYAPP.Models.SubModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models
{
    public class Album : BaseEntity
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = default!;

        [StringLength(200)]
        public string? Slug { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ReleaseDate { get; set; }

        [StringLength(255)]
        public string? Label { get; set; }

        public string? ArtistId { get; set; }

        [ForeignKey(nameof(ArtistId))]
        public virtual Artist? Artist { get; set; }

        public virtual ICollection<Song>? Songs { get; set; }
    }
}