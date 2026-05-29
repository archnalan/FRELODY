namespace FRELODYSHRD.Dtos
{
    /// <summary>Public PayPal config for the browser SDK.</summary>
    public class PayPalConfigDto
    {
        public string ClientId { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        /// <summary>False when no PayPal credentials are configured (hide the button).</summary>
        public bool Enabled { get; set; }
    }

    public class PayPalCreateOrderRequest
    {
        public string ProductId { get; set; } = default!;
    }

    public class PayPalCreateOrderResult
    {
        public string OrderId { get; set; } = default!;
        /// <summary>Amount the buyer will be charged, in <see cref="Currency"/>.</summary>
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
    }

    public class PayPalCaptureRequest
    {
        public string OrderId { get; set; } = default!;
        public string ProductId { get; set; } = default!;
    }

    public class PayPalCaptureResult
    {
        public bool Success { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
    }
}
