using FRELODYAPP.Dtos.SubDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.PesaPalDtos
{
    public class OrderDetailDto : BaseEntityDto
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
