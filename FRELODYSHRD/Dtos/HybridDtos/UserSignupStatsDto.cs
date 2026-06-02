using System;
using System.Collections.Generic;

namespace FRELODYSHRD.Dtos.HybridDtos
{
    public class UserSignupStatsDto
    {
        public Dictionary<DateTime, int> NewByDate { get; set; } = new();        // new accounts created each day in window
        public Dictionary<DateTime, int> CumulativeByDate { get; set; } = new(); // running total (incl. baseline of accounts before window start)
        public int TotalUsers { get; set; }                                      // all-time total end users
        public int NewInWindow { get; set; }                                     // sum of NewByDate
    }
}
