using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Models
{
    public class BookCategoryPairViewModel
    {
        public string SongBookId { get; set; } = default!;
        public string BookTitle { get; set; } = default!;
        public string? CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }

}
