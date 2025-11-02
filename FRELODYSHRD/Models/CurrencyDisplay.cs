using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Models
{
    public class CurrencyDisplayInfo
    {
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string FormattedAmount { get; set; } = string.Empty;
        public bool IsConverted { get; set; }
        public decimal? OriginalAmount { get; set; }
        public string? OriginalCurrency { get; set; }
    }
}
