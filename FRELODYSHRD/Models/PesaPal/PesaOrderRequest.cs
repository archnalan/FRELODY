using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FRELODYSHRD.Models.PesaPal
{
    public class PesaOrderRequest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("callback_url")]
        public string? CallbackUrl { get; set; }

        [JsonPropertyName("notification_id")]
        public string NotificationId { get; set; } = string.Empty;

        [JsonPropertyName("billing_address")]
        public BillingAddress BillingAddress { get; set; } = new();

        [JsonPropertyName("account_number")]
        public string? AccountNumber { get; set; }

        [JsonPropertyName("subscription_details")]
        public SubscriptionDetails? SubscriptionDetails { get; set; }
    }
}
