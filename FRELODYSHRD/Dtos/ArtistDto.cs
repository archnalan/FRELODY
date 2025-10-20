using FRELODYAPP.Dtos.SubDtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos
{
    public class ArtistDto : BaseEntityDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = default!;

        [StringLength(255)]
        public string? Bio { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        public virtual ICollection<AlbumDto>? Albums { get; set; }
    }
}
