using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.Constants;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FRELODYLIB.Models
{
    public class Payment:BaseEntity
    {
        public string OrderId { get; set; } =string.Empty;

        public string OrderTrackingId { get; set; } = string.Empty;

        public string MerchantReference { get; set; } = string.Empty; 

        public string? PaymentMethod { get; set; }

        public decimal? Amount { get; set; }

        public string? Currency { get; set; } = "UGX";

        public PaymentStatus? Status { get; set; } = PaymentStatus.PENDING;

        public string? ConfirmationCode { get; set; }

        public string? PaymentAccount { get; set; }

        public string? Description { get; set; }

        public string? Message { get; set; }

        public DateTimeOffset? CreatedDate { get; set; } 

        public DateTimeOffset? CompletedDate { get; set; }

        public string? RawResponse { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }
    }
}
