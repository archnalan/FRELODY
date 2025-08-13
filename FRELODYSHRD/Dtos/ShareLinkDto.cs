using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos
{
    public class ShareLinkDto
    {
        public string Id { get; set; } = string.Empty;
        public string SongId { get; set; } = string.Empty;
        public string ShareToken { get; set; } = string.Empty;
        public string ShareUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class ShareLinkCreateDto
    {
        public string SongId { get; set; } = string.Empty;
        public int? ExpirationDays { get; set; } = 30; // Default 30 days
    }
}
