using DocumentFormat.OpenXml.Drawing.Charts;
using FRELODYSHRD.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FRELODYSHRD.Models.PesaPal
{
    public class PesaPayment
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("orderMerchantReference")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("orderTrackingId")]
        public string OrderTrackingId { get; set; } = string.Empty;

        [JsonPropertyName("merchant_reference")]
        public string MerchantReference { get; set; } = string.Empty;

        [JsonPropertyName("payment_method")]
        public string? PaymentMethod { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "UGX";

        [JsonPropertyName("payment_status_description")]
        public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;

        [JsonPropertyName("confirmation_code")]
        public string? ConfirmationCode { get; set; }

        [JsonPropertyName("payment_account")]
        public string? PaymentAccount { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("created_date")]
        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? CompletedDate { get; set; }

        public string? RawResponse { get; set; }
    }
}
