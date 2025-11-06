using FRELODYAPP.Models.SubModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYLIB.Models
{
    public class SongRecovery:BaseEntity
    {
        public string SongId { get; set; } = string.Empty;
        public string RecoveryName { get; set; } = string.Empty;
        public DateTimeOffset? RecoveryTimeStamp { get; set; }
    }
}
