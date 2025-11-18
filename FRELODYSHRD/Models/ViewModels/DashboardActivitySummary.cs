using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Models.ViewModels
{
    public class DashboardActivitySummary
    {
        public int NewSongsCount { get; set; }
        public int UpdatedSongsCount { get; set; }
        public int NewPlaylistsCount { get; set; }
        public DateTimeOffset LastLoginTime { get; set; }
        public List<SummarySongDto> NewPublicSongs { get; set; } = new();
    }
}
