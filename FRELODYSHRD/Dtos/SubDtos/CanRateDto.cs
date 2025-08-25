using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.SubDtos
{
    public class CanRateDto
    {
        public bool CanRate { get; set; }
        public string? Reason { get; set; }
        public decimal? YourRating { get; set; }
        public decimal? AggregateRating { get; set; }
        public int TotalRatings { get; set; }
        public int MaxEdits { get; set; } = 3;
        public int EditsRemaining { get; set; }
    }
}
