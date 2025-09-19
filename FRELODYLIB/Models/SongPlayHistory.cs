using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYLIB.Models
{
    public class SongPlayHistory : BaseEntity
    {
        public string SongId { get; set; } = default!;

        public string? UserId { get; set; } 

        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
        
        [StringLength(50)]// (e.g., "SongList", "Search", "Favorites")
        public string? PlaySource { get; set; }
        
        [StringLength(50)]
        public string? SessionId { get; set; }

        // Navigation properties
        public virtual Song? Song { get; set; } = default!;
        public virtual User? User { get; set; } = default!;
    }
}
