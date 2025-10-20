using FRELODYAPP.Models.SubModels;
using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Models
{
    public class Artist : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = default!;

        [StringLength(200)]
        public string? Slug { get; set; }

        [StringLength(255)]
        public string? Bio { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        public virtual ICollection<Album>? Albums { get; set; }
    }
}