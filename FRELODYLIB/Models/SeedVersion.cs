using System.ComponentModel.DataAnnotations;
using FRELODYAPP.Models.SubModels;

namespace FRELODYAPP.Models
{
    public class SeedVersion : BaseEntity
    {
        [Required]
        [StringLength(64)]
        public string SeedName { get; set; } = null!;

        [Required]
        [StringLength(128)]
        public string Version { get; set; } = null!;

        public DateTime SeededAt { get; set; }
    }
}
