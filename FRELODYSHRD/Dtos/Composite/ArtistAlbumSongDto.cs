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

namespace FRELODYSHRD.Dtos.Composite
{
    public class ArtistAlbumSongDto : BaseEntityDto
    {
        [Required]
        public string ArtistId { get; set; } = default!;

        [Required]
        public string SongId { get; set; } = default!;

        public string? AlbumId { get; set; }
        public int? DisplayOrder { get; set; }

        public string? Role { get; set; } // e.g., "Primary Artist", "Featured", "Cover"

        public int? TrackNumber { get; set; } // Track number if part of an album

        public virtual Artist? Artist { get; set; }

        public virtual SongDto? Song { get; set; }

        public virtual AlbumDto? Album { get; set; }
    }
}
