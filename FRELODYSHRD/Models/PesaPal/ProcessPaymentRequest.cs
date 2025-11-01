using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FRELODYSHRD.Dtos.PesaPalDtos;

namespace FRELODYSHRD.Models.PesaPal
{
    public class ProcessPaymentRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public List<OrderDetailDto> OrderDetails { get; set; } = new();
        public BillingAddress BillingAddress { get; set; } = new();
        public string CallbackUrl { get; set; } = string.Empty;
        public string IpnId { get; set; } = string.Empty;
        public SubscriptionDetails? SubscriptionDetails { get; set; }
    }
}
