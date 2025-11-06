using FRELODYAPP.Dtos.SubDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos
{
    public class SongRecoveryDto : BaseEntityDto
    {
        public string? SongId { get; set; } = string.Empty;
        public string RecoveryName { get; set; } = string.Empty;
        public DateTimeOffset? RecoveryTimeStamp { get; set; }
    }
}
