using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FRELODYSHRD.Models.PesaPal
{
    public class PesaOrderResponse
    {
        [JsonPropertyName("order_tracking_id")]
        public string OrderTrackingId { get; set; } = string.Empty;

        [JsonPropertyName("merchant_reference")]
        public string MerchantReference { get; set; } = string.Empty;

        [JsonPropertyName("redirect_url")]
        public string RedirectUrl { get; set; } = string.Empty;

        [JsonPropertyName("error")]
        public Error? Error { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
