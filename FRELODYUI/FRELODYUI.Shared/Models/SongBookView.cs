using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Models
{
    public class SongBookView
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool IsExpanded { get; set; }
        public List<CategoryView> Categories { get; set; } = new List<CategoryView>();
        public int SongCount => Categories.Sum(c => c.Songs.Count);
    }
}
