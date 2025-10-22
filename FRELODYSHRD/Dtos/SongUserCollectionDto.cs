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
    public class SongUserCollectionDto : BaseEntityDto
    {
        public string SongId { get; set; } = default!;
        public string SongCollectionId { get; set; } = default!;
        public string? AddedByUserId { get; set; }
        public int? SortOrder { get; set; }
        public DateTimeOffset DateScheduled { get; set; } = DateTimeOffset.UtcNow;
        public virtual SongDto? Song { get; set; }
        public virtual PlaylistDto? SongCollection { get; set; }
    }
}
