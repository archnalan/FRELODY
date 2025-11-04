using DocumentFormat.OpenXml.Drawing.Charts;
using FRELODYAPP.Dtos.SubDtos;
using FRELODYSHRD.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos
{
    public class PaymentDto:BaseEntityDto
    {
        public string OrderId { get; set; } = string.Empty;

        public string OrderTrackingId { get; set; } = string.Empty;

        public string MerchantReference { get; set; } = string.Empty;

        public string? PaymentMethod { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "UGX";

        public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;

        public string? ConfirmationCode { get; set; }

        public string? PaymentAccount { get; set; }

        public string? Description { get; set; }

        public string? Message { get; set; }

        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? CompletedDate { get; set; }

        public string? RawResponse { get; set; }

        public virtual Order? Order { get; set; }
    }
}
