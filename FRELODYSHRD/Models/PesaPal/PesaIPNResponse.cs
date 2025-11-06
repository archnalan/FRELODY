using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FRELODYSHRD.Models.PesaPal
{
    public class PesaIPNResponse
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("created_date")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("ipn_id")]
        public string IpnId { get; set; } = string.Empty;

        [JsonPropertyName("notification_type")]
        public int NotificationType { get; set; }

        [JsonPropertyName("ipn_notification_type_description")]
        public string IpnNotificationTypeDescription { get; set; } = string.Empty;

        [JsonPropertyName("ipn_status")]
        public int IpnStatus { get; set; }

        [JsonPropertyName("ipn_status_decription")]
        public string IpnStatusDescription { get; set; } = string.Empty;

        [JsonPropertyName("error")]
        public Error? Error { get; set; } = null;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
