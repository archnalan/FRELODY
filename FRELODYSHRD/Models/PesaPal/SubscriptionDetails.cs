using System.Text.Json.Serialization;

namespace FRELODYSHRD.Models.PesaPal
{
    public class SubscriptionDetails
    {
        [JsonPropertyName("start_date")]
        public string? StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public string? EndDate { get; set; }

        [JsonPropertyName("frequency")]
        public string? Frequency { get; set; }
    }
}
