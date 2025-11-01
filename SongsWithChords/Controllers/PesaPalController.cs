using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYSHRD.Models.PesaPal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PesaPalController : ControllerBase
    {
        private readonly IPesaPalService _pesaPalService;
        private readonly ILogger<PesaPalController> _logger;

        public PesaPalController(IPesaPalService pesaPalService, ILogger<PesaPalController> logger)
        {
            _pesaPalService = pesaPalService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate with PesaPal and get bearer token
        /// </summary>
        [HttpPost("authenticate")]
        [ProducesResponseType(typeof(PesaAuthResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Authenticate()
        {
            var result = await _pesaPalService.AuthenticateAsync();

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, new { error = result.Error?.Message });
            }

            return Ok(result.Data);
        }

        /// <summary>
        /// Register IPN URL for payment notifications
        /// </summary>
        [HttpPost("register-ipn")]
        [ProducesResponseType(typeof(PesaIPNResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RegisterIPN([FromBody] RegisterIPNRequest request)
        {
            var result = await _pesaPalService.RegisterIPNAsync(request.IpnUrl, request.NotificationType ?? "POST");

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, new { error = result.Error?.Message });
            }

            return Ok(result.Data);
        }

        /// <summary>
        /// Get list of registered IPNs
        /// </summary>
        [HttpGet("ipn-list")]
        [ProducesResponseType(typeof(List<PesaIPNResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetIPNList()
        {
            var result = await _pesaPalService.GetRegisteredIPNsAsync();

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, new { error = result.Error?.Message });
            }

            return Ok(result.Data);
        }

        /// <summary>
        /// Submit order and initiate payment
        /// </summary>
        [HttpPost("submit-order")]
        [ProducesResponseType(typeof(PesaOrderResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SubmitOrder([FromBody] PesaOrderRequest orderRequest)
        {
            var result = await _pesaPalService.SubmitOrderAsync(orderRequest);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, new { error = result.Error?.Message });
            }

            return Ok(result.Data);
        }

        /// <summary>
        /// Get transaction status by order tracking ID
        /// </summary>
        [HttpGet("transaction-status/{orderTrackingId}")]
        [ProducesResponseType(typeof(TransactionStatusResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactionStatus(string orderTrackingId)
        {
            var result = await _pesaPalService.GetTransactionStatusAsync(orderTrackingId);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, new { error = result.Error?.Message });
            }

            return Ok(result.Data);
        }

        /// <summary>
        /// Handle IPN callback from PesaPal
        /// </summary>
        [HttpPost("ipn-callback")]
        [HttpGet("ipn-callback")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> IPNCallback(
            [FromQuery] string OrderTrackingId,
            [FromQuery] string OrderMerchantReference,
            [FromQuery] string OrderNotificationType)
        {
            _logger.LogInformation(
                "IPN Callback received - OrderTrackingId: {OrderTrackingId}, MerchantReference: {OrderMerchantReference}, NotificationType: {NotificationType}",
                OrderTrackingId,
                OrderMerchantReference,
                OrderNotificationType);

            var result = await _pesaPalService.ProcessIPNNotificationAsync(OrderTrackingId, OrderMerchantReference);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, new { error = result.Error?.Message });
            }

            return Ok(new { message = "IPN processed successfully", success = result.Data });
        }

        /// <summary>
        /// Handle payment callback redirect from PesaPal
        /// </summary>
        [HttpGet("payment-callback")]
        public async Task<IActionResult> PaymentCallback(
            [FromQuery] string OrderTrackingId,
            [FromQuery] string OrderMerchantReference)
        {
            _logger.LogInformation(
                "Payment Callback received - OrderTrackingId: {OrderTrackingId}, MerchantReference: {OrderMerchantReference}",
                OrderTrackingId,
                OrderMerchantReference);

            if (string.IsNullOrEmpty(OrderTrackingId))
            {
                return Redirect("/payment-failed?error=missing-tracking-id");
            }

            var result = await _pesaPalService.GetTransactionStatusAsync(OrderTrackingId);

            if (!result.IsSuccess || result.Data == null)
            {
                return Redirect($"/payment-failed?orderId={OrderMerchantReference}&error=status-check-failed");
            }

            return result.Data.PaymentStatusDescription?.ToUpperInvariant() switch
            {
                "COMPLETED" => Redirect($"/payment-success?orderId={OrderMerchantReference}"),
                "FAILED" => Redirect($"/payment-failed?orderId={OrderMerchantReference}"),
                "CANCELLED" => Redirect($"/payment-cancelled?orderId={OrderMerchantReference}"),
                _ => Redirect($"/payment-pending?orderId={OrderMerchantReference}")
            };
        }

        /// <summary>
        /// Complete workflow: Create order and process payment
        /// </summary>
        [HttpPost("process-payment")]
        [ProducesResponseType(typeof(PesaOrderResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
        {
            var result = await _pesaPalService.ProcessOrderPaymentAsync(
                request.CustomerId,
                request.OrderDetails,
                request.BillingAddress,
                request.CallbackUrl,
                request.IpnId,
                request.SubscriptionDetails);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, new { error = result.Error?.Message });
            }

            return Ok(result.Data);
        }
    }
}
