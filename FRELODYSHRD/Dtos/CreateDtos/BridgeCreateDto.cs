using FRELODYAPP.Dtos;
using FRELODYLIB.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class BridgeCreateDto : ISongPartDto
    {
        public Guid? SongId { get; set; }
        public string Title { get; set; }
        public int BridgeNumber { get; set; }
        public int? RepeatCount { get; set; }
        public ICollection<LineCreateDto>? LyricLines { get; set; }

        public int GetPartNumber()
        {
            return BridgeNumber;
        }
    }
}
