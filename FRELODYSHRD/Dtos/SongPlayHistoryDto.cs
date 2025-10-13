using FRELODYAPP.Dtos.SubDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos
{
    public class SongPlayHistoryDto : BaseEntityDto
    {
        public string SongId { get; set; } = default!;
        public string UserId { get; set; } = default!;
        public DateTimeOffset PlayedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? PlaySource { get; set; }
        public string? SessionId { get; set; }

        //Details for reporting
        public string? SongTitle { get; set; }
        public int? SongNumber { get; set; }
    }
}
