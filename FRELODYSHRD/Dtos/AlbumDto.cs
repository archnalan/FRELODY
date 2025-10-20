using DocumentFormat.OpenXml.Bibliography;
using FRELODYAPP.Dtos;
using FRELODYAPP.Dtos.SubDtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos
{
    public class AlbumDto : BaseEntityDto
    {
        [Required]
        [StringLength(150)]
        public string Title { get; set; } = default!;

        [StringLength(200)]
        public string? Slug { get; set; }

        public DateTime? ReleaseDate { get; set; }

        [StringLength(255)]
        public string? Label { get; set; }

        public string? ArtistId { get; set; }

        public virtual Artist? Artist { get; set; }

        public virtual ICollection<SongDto>? Songs { get; set; }
    }
}
