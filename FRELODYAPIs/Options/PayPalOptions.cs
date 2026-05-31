namespace FRELODYAPIs.Options
{
    /// <summary>
    /// PayPal one-time checkout (Orders v2) configuration. Bound from the "PayPal"
    /// section. ClientId is public (sent to the browser SDK); ClientSecret is not.
    /// </summary>
    public sealed class PayPalOptions
    {
        public const string SectionName = "PayPal";

        /// <summary>"sandbox" or "live" — selects the api-m base URL.</summary>
        public string Mode { get; set; } = "sandbox";

        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>Settlement currency. PayPal does not support UGX, so charges are USD.</summary>
        public string Currency { get; set; } = "USD";

        // Prices live on the Product (Product.PriceUsd) — the single source of truth — not here.

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

        public string ApiBaseUrl => string.Equals(Mode, "live", StringComparison.OrdinalIgnoreCase)
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";
    }
}
