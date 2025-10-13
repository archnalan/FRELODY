using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYLIB.Models
{
    [Index(nameof(SongId), nameof(UserId), nameof(TenantId), IsUnique = true)] 
    public class SongUserFavorite : BaseEntity
    {
        [Required]
        public string SongId { get; set; } = default!;

        [Required] 
        public string UserId { get; set; } = default!;

        public DateTimeOffset FavoritedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual Song? Song { get; set; }
        public virtual User? User { get; set; }
    }
}
