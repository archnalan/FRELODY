using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPIs.Options;
using FRELODYAPIs.Services.PayPal;
using FRELODYAPP.Data.Infrastructure;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class PayPalService : IPayPalService
    {
        private readonly SongDbContext _context;
        private readonly PayPalClient _client;
        private readonly IBillingActivationService _billing;
        private readonly ITenantProvider _tenantProvider;
        private readonly PayPalOptions _options;
        private readonly ILogger<PayPalService> _logger;

        public PayPalService(
            SongDbContext context,
            PayPalClient client,
            IBillingActivationService billing,
            ITenantProvider tenantProvider,
            IOptions<PayPalOptions> options,
            ILogger<PayPalService> logger)
        {
            _context = context;
            _client = client;
            _billing = billing;
            _tenantProvider = tenantProvider;
            _options = options.Value;
            _logger = logger;
        }

        public PayPalConfigDto GetConfig() => new()
        {
            ClientId = _options.IsConfigured ? _options.ClientId : string.Empty,
            Currency = _options.Currency,
            Enabled = _options.IsConfigured
        };

        public async Task<ServiceResult<PayPalCreateOrderResult>> CreateOrderAsync(string productId)
        {
            try
            {
                if (!_options.IsConfigured)
                    return ServiceResult<PayPalCreateOrderResult>.Failure(
                        new ServerErrorException("PayPal is not configured."));

                var userId = _tenantProvider.GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return ServiceResult<PayPalCreateOrderResult>.Failure(
                        new UnAuthorizedException("Sign in to continue."));

                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
                if (product is null)
                    return ServiceResult<PayPalCreateOrderResult>.Failure(
                        new NotFoundException("Plan not found."));

                var amount = ResolveUsdAmount(product);
                if (amount <= 0)
                    return ServiceResult<PayPalCreateOrderResult>.Failure(
                        new BadRequestException("This plan is not purchasable via PayPal."));

                var orderId = await _client.CreateOrderAsync(
                    amount, _options.Currency,
                    customId: $"{userId}|{product.Id}",
                    description: $"FRELODY {product.Name}",
                    CancellationToken.None);

                return ServiceResult<PayPalCreateOrderResult>.Success(new PayPalCreateOrderResult
                {
                    OrderId = orderId,
                    Amount = amount,
                    Currency = _options.Currency
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal order for product {ProductId}", productId);
                return ServiceResult<PayPalCreateOrderResult>.Failure(
                    new ServerErrorException("Could not start the PayPal payment."));
            }
        }

        public async Task<ServiceResult<PayPalCaptureResult>> CaptureOrderAsync(string orderId, string productId)
        {
            try
            {
                if (!_options.IsConfigured)
                    return ServiceResult<PayPalCaptureResult>.Failure(
                        new ServerErrorException("PayPal is not configured."));

                var userId = _tenantProvider.GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return ServiceResult<PayPalCaptureResult>.Failure(
                        new UnAuthorizedException("Sign in to continue."));

                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
                if (product is null)
                    return ServiceResult<PayPalCaptureResult>.Failure(
                        new NotFoundException("Plan not found."));

                var outcome = await _client.CaptureOrderAsync(orderId, CancellationToken.None);

                if (!string.Equals(outcome.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("PayPal capture for {OrderId} returned {Status}", orderId, outcome.Status);
                    return ServiceResult<PayPalCaptureResult>.Success(new PayPalCaptureResult
                    {
                        Success = false,
                        Status = outcome.Status,
                        Message = "Payment was not completed."
                    });
                }

                var amount = ResolveUsdAmount(product);
                var now = DateTimeOffset.UtcNow;

                var order = new Order
                {
                    CustomerId = userId,
                    TotalAmout = amount,
                    Status = OrderStatus.COMPLETED,
                    OrderDate = now,
                    OrderNote = $"PayPal {product.Name}"
                };
                _context.Orders.Add(order);

                _context.Payments.Add(new Payment
                {
                    OrderId = order.Id,
                    OrderTrackingId = orderId,
                    MerchantReference = order.Id,
                    PaymentMethod = "PayPal",
                    Amount = amount,
                    Currency = _options.Currency,
                    Status = PaymentStatus.COMPLETED,
                    ConfirmationCode = outcome.CaptureId,
                    Description = $"FRELODY {product.Name}",
                    CreatedDate = now,
                    CompletedDate = now,
                    RawResponse = outcome.Raw
                });

                await _context.SaveChangesAsync();

                // Grant premium. Even if this somehow fails, the payment is recorded.
                var activation = await _billing.ActivatePremiumAsync(userId, product);
                if (!activation.IsSuccess)
                    _logger.LogError("Premium activation failed after PayPal capture for user {UserId}, order {OrderId}",
                        userId, orderId);

                return ServiceResult<PayPalCaptureResult>.Success(new PayPalCaptureResult
                {
                    Success = true,
                    Status = outcome.Status,
                    Message = "Payment complete."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing PayPal order {OrderId}", orderId);
                return ServiceResult<PayPalCaptureResult>.Failure(
                    new ServerErrorException("Could not complete the PayPal payment."));
            }
        }

        // The product row is the single source of truth for pricing; PayPal charges its USD price.
        private static decimal ResolveUsdAmount(Product product) => product.PriceUsd ?? 0m;
    }
}
