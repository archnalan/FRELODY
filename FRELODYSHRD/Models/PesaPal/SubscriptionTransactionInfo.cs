using System.Text.Json.Serialization;

namespace FRELODYSHRD.Models.PesaPal
{
    public class SubscriptionTransactionInfo
    {
        [JsonPropertyName("subscription_reference")]
        public string? SubscriptionReference { get; set; }

        [JsonPropertyName("subscription_status")]
        public string? SubscriptionStatus { get; set; }

        [JsonPropertyName("subscription_start_date")]
        public DateTime? SubscriptionStartDate { get; set; }

        [JsonPropertyName("subscription_end_date")]
        public DateTime? SubscriptionEndDate { get; set; }

        [JsonPropertyName("frequency")]
        public string? Frequency { get; set; }
    }
}
