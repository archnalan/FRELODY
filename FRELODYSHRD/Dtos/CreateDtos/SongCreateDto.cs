using FRELODYAPP.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class SongCreateDto
    {

        [Required]
        [Display(Name = "SDAH-")]
        public int Number { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        public string? Slug { get; set; }
    }
}
