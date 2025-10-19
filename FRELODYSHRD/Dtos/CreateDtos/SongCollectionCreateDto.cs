using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class SongCollectionCreateDto
    {
        public string Title { get; set; }
        public List<string> SongIds { get; set; }
        public string? Theme { get; set; }
        public string? Curator { get; set; }
        public DateTimeOffset? SheduledDate { get; set; } = DateTimeOffset.Now;
    }
}
