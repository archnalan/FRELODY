using FRELODYSHRD.Dtos.PesaPalDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Models.PesaPal
{
    public class InitiatePesaPalDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string Currency { get; set; } = "UGX";
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string CallbackUrl { get; set; } = string.Empty;
        public string? IpnCallbackUrl { get; set; }
        public BillingAddress BillingAddress { get; set; } = new();
        public SubscriptionDetails? SubscriptionDetails { get; set; }
    }
}
