using FRELODYAPP.Models.SubModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYLIB.Models
{
    public class OrderDetail:BaseEntity
    {
        public string OrderId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal? DiscountValue { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? DetailNote { get; set; }
    }
}
