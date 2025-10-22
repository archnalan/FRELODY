using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.SubDtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.Composite
{
    public class BookCategorySongDto : BaseEntityDto
    {
        [Required]
        public string SongBookId { get; set; } = default!;

        [Required]
        public string SongId { get; set; } = default!;
        public string? CategoryId { get; set; }
        public int? DisplayOrder { get; set; }

        [Range(1, 9999)]
        public int? SongNumber { get; set; }
        public virtual SongBookDto? SongBook { get; set; }
        public virtual SongDto? Song { get; set; }
        public virtual CategoryDto? Category { get; set; }
    }
}
