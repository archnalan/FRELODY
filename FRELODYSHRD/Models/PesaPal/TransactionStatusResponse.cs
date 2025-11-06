using System.Text.Json.Serialization;

namespace FRELODYSHRD.Models.PesaPal
{
    public class TransactionStatusResponse
    {
        [JsonPropertyName("payment_method")]
        public string? PaymentMethod { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("created_date")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("confirmation_code")]
        public string? ConfirmationCode { get; set; }

        [JsonPropertyName("payment_status_description")]
        public string? PaymentStatusDescription { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("payment_account")]
        public string? PaymentAccount { get; set; }

        [JsonPropertyName("call_back_url")]
        public string? CallbackUrl { get; set; }

        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("merchant_reference")]
        public string? MerchantReference { get; set; }

        [JsonPropertyName("payment_status_code")]
        public string? PaymentStatusCode { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("error")]
        public Error? Error { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("subscription_transaction_info")]
        public SubscriptionTransactionInfo? SubscriptionTransactionInfo { get; set; }
    }
}
