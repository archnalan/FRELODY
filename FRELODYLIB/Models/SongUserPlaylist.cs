using FRELODYAPP.Models;
using FRELODYAPP.Models.SubModels;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYLIB.Models
{
    [Index(nameof(SongId), nameof(PlaylistId), nameof(TenantId), IsUnique = true)]
    public class SongUserPlaylist : BaseEntity
    {
        [Required]
        public string SongId { get; set; } = default!;

        [Required]
        public string PlaylistId { get; set; } = default!;

        public string? AddedByUserId { get; set; }
        public int? SortOrder { get; set; }
        public DateTimeOffset DateScheduled { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(SongId))]
        public virtual Song? Song { get; set; }

        [ForeignKey(nameof(PlaylistId))]
        public virtual Playlist? SongPlaylist { get; set; }
    }
}