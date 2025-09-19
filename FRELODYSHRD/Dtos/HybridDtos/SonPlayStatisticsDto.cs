using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.HybridDtos
{
    public class SongPlayStatisticsDto
    {
        public int TotalPlays { get; set; }
        public int UniqueSongs { get; set; }
        public DateTime? FirstPlay { get; set; }
        public DateTime? LastPlay { get; set; }
        public string? MostPlayedSongTitle { get; set; }
        public int MostPlayedSongCount { get; set; }
        public Dictionary<string, int> PlaysBySource { get; set; } = new();
        public Dictionary<DateTime, int> PlaysByDate { get; set; } = new();
    }
}
