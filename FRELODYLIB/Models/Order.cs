using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYLIB.Models
{
    public class Order:BaseEntity
    {
        public decimal TotalAmout { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public OrderStatus? Status { get; set; }
        public DateTimeOffset? OrderDate { get; set; }
        public string? OrderNote { get; set; }
    }
}
