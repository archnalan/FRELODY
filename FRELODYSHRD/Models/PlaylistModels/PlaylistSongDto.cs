using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Models.PlaylistModels
{
    public class PlaylistSongDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int? SongNumber { get; set; }
        public string? WrittenBy { get; set; }
        public int? SortOrder { get; set; }
        public DateTimeOffset DateScheduled { get; set; }
    }
}
