using FRELODYAPP.Dtos.SubDtos;
using FRELODYSHRD.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.PesaPalDtos
{
    public class OrderDto : BaseEntityDto
    {
        public decimal? TotalAmout { get; set; }
        public OrderStatus? Status { get; set; }
        public DateTimeOffset? OrderDate { get; set; }
        public string? CustomerId { get; set; }
        public string? OrderNote { get; set; }
    }
}
