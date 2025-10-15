using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Models
{
    public class SearchResult
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int? SongNumber { get; set; }
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string MatchType { get; set; } = string.Empty;
        public string MatchSnippet { get; set; } = string.Empty;
        public int RelevanceScore { get; set; }
    }
}
