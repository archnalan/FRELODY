using System;
using System.Collections.Generic;

namespace FRELODYSHRD.Dtos.HybridDtos
{
    public class RevenueStatsDto
    {
        // currency code (e.g. "USD","UGX") -> (day -> summed amount of COMPLETED payments)
        public Dictionary<string, Dictionary<DateTime, decimal>> RevenueByCurrency { get; set; } = new();
        // currency code -> total summed amount within the window
        public Dictionary<string, decimal> TotalsByCurrency { get; set; } = new();
    }
}
