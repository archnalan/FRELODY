using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Interfaces;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.PesaPalDtos;
using FRELODYSHRD.Models.PesaPal;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class PesaPalService : IPesaPalService
    {
        private readonly SongDbContext _context;
        private readonly ILogger<PesaPalService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly string _sandboxUrl;
        private readonly bool _useSandbox;

        private string? _cachedToken;
        private DateTime? _tokenExpiry;

        public PesaPalService(
            SongDbContext context,
            ILogger<PesaPalService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;

            _baseUrl = _configuration["PesaPal:BaseUrl"] ?? "https://pay.pesapal.com/v3";
            _sandboxUrl = _configuration["PesaPal:SandboxUrl"] ?? "https://cybqa.pesapal.com/pesapalv3";
            _useSandbox = _configuration.GetValue<bool>("PesaPal:UseSandbox", true);
        }

        private string ApiBaseUrl => _useSandbox ? _sandboxUrl : _baseUrl;

        public async Task<ServiceResult<PesaAuthResponse>> AuthenticateAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiry.HasValue && _tokenExpiry > DateTime.UtcNow.AddMinutes(1))
                {
                    return ServiceResult<PesaAuthResponse>.Success(new PesaAuthResponse
                    {
                        Token = _cachedToken,
                        ExpiryDate = _tokenExpiry,
                        Message = "Using cached token"
                    });
                }

                var consumerKey = _configuration["UgandanMerchant:consumer_key"];
                var consumerSecret = _configuration["UgandanMerchant:consumer_secret"];

                if (string.IsNullOrEmpty(consumerKey) || string.IsNullOrEmpty(consumerSecret))
                {
                    _logger.LogError("PesaPal credentials not configured");
                    return ServiceResult<PesaAuthResponse>.Failure(
                        new ServerErrorException("PesaPal credentials are not configured"));
                }

                var client = _httpClientFactory.CreateClient();
                var payload = new
                {
                    consumer_key = consumerKey,
                    consumer_secret = consumerSecret
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync($"{ApiBaseUrl}/api/Auth/RequestToken", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Authentication failed: {StatusCode} - {Response}", response.StatusCode, responseContent);
                    return ServiceResult<PesaAuthResponse>.Failure(
                        new UnAuthorizedException("Failed to authenticate with PesaPal"));
                }

                var authResponse = JsonSerializer.Deserialize<PesaAuthResponse>(responseContent);

                if (authResponse != null && !string.IsNullOrEmpty(authResponse.Token))
                {
                    _cachedToken = authResponse.Token;
                    _tokenExpiry = authResponse.ExpiryDate ?? DateTime.UtcNow.AddMinutes(5);
                    _logger.LogInformation("Successfully authenticated with PesaPal");
                    return ServiceResult<PesaAuthResponse>.Success(authResponse);
                }

                return ServiceResult<PesaAuthResponse>.Failure(
                    new ServerErrorException("Invalid authentication response from PesaPal"));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error during PesaPal authentication");
                return ServiceResult<PesaAuthResponse>.Failure(
                    new ServerErrorException("Network error connecting to PesaPal"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PesaPal authentication");
                return ServiceResult<PesaAuthResponse>.Failure(
                    new ServerErrorException("An unexpected error occurred during authentication"));
            }
        }

        public async Task<ServiceResult<PesaIPNResponse>> RegisterIPNAsync(string ipnUrl, string notificationType = "POST")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ipnUrl))
                {
                    return ServiceResult<PesaIPNResponse>.Failure(
                        new BadRequestException("IPN URL cannot be null or empty"));
                }

                if (!Uri.TryCreate(ipnUrl, UriKind.Absolute, out var uriResult) ||
                    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                {
                    return ServiceResult<PesaIPNResponse>.Failure(
                        new BadRequestException("IPN URL must be a valid HTTP or HTTPS URL"));
                }

                var authResult = await AuthenticateAsync();
                if (!authResult.IsSuccess || authResult.Data == null)
                {
                    return ServiceResult<PesaIPNResponse>.Failure(
                        authResult.Error ?? new UnAuthorizedException("Authentication failed"));
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Data.Token);

                var payload = new
                {
                    url = ipnUrl,
                    ipn_notification_type = notificationType
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync($"{ApiBaseUrl}/api/URLSetup/RegisterIPN", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("IPN registration failed: {StatusCode} - {Response}", response.StatusCode, responseContent);
                    return ServiceResult<PesaIPNResponse>.Failure(
                        new ServerErrorException($"Failed to register IPN URL: {response.StatusCode}"));
                }

                var ipnResponse = JsonSerializer.Deserialize<PesaIPNResponse>(responseContent);

                if (ipnResponse == null)
                {
                    return ServiceResult<PesaIPNResponse>.Failure(
                        new ServerErrorException("Invalid response from PesaPal"));
                }

                _logger.LogInformation("Successfully registered IPN URL: {IpnId}", ipnResponse.IpnId);
                return ServiceResult<PesaIPNResponse>.Success(ipnResponse);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error registering IPN URL");
                return ServiceResult<PesaIPNResponse>.Failure(
                    new ServerErrorException("Network error connecting to PesaPal"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering IPN URL");
                return ServiceResult<PesaIPNResponse>.Failure(
                    new ServerErrorException("An unexpected error occurred while registering IPN"));
            }
        }

        public async Task<ServiceResult<List<PesaIPNResponse>>> GetRegisteredIPNsAsync()
        {
            try
            {
                var authResult = await AuthenticateAsync();
                if (!authResult.IsSuccess || authResult.Data == null)
                {
                    return ServiceResult<List<PesaIPNResponse>>.Failure(
                        authResult.Error ?? new UnAuthorizedException("Authentication failed"));
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Data.Token);

                var response = await client.GetAsync($"{ApiBaseUrl}/api/URLSetup/GetIpnList");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get IPN list: {StatusCode}", response.StatusCode);
                    return ServiceResult<List<PesaIPNResponse>>.Failure(
                        new ServerErrorException($"Failed to retrieve IPN list: {response.StatusCode}"));
                }

                var ipnList = JsonSerializer.Deserialize<List<PesaIPNResponse>>(responseContent) ?? new List<PesaIPNResponse>();
                return ServiceResult<List<PesaIPNResponse>>.Success(ipnList);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error getting IPN list");
                return ServiceResult<List<PesaIPNResponse>>.Failure(
                    new ServerErrorException("Network error connecting to PesaPal"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting IPN list");
                return ServiceResult<List<PesaIPNResponse>>.Failure(
                    new ServerErrorException("An unexpected error occurred while retrieving IPN list"));
            }
        }

        public async Task<ServiceResult<PesaOrderResponse>> SubmitOrderAsync(PesaOrderRequest orderRequest)
        {
            try
            {
                if (orderRequest == null)
                {
                    return ServiceResult<PesaOrderResponse>.Failure(
                        new BadRequestException("Order request cannot be null"));
                }

                if (string.IsNullOrWhiteSpace(orderRequest.Id))
                {
                    return ServiceResult<PesaOrderResponse>.Failure(
                        new BadRequestException("Order ID is required"));
                }

                if (orderRequest.Amount <= 0)
                {
                    return ServiceResult<PesaOrderResponse>.Failure(
                        new BadRequestException("Order amount must be greater than zero"));
                }

                var authResult = await AuthenticateAsync();
                if (!authResult.IsSuccess || authResult.Data == null)
                {
                    return ServiceResult<PesaOrderResponse>.Failure(
                        authResult.Error ?? new UnAuthorizedException("Authentication failed"));
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Data.Token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent(
                    JsonSerializer.Serialize(orderRequest),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync($"{ApiBaseUrl}/api/Transactions/SubmitOrderRequest", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Order submission failed: {StatusCode} - {Response}", response.StatusCode, responseContent);
                    return ServiceResult<PesaOrderResponse>.Failure(
                        new ServerErrorException($"Failed to submit order: {response.StatusCode}"));
                }

                var orderResponse = JsonSerializer.Deserialize<PesaOrderResponse>(responseContent);

                if (orderResponse == null)
                {
                    return ServiceResult<PesaOrderResponse>.Failure(
                        new ServerErrorException("Invalid response from PesaPal"));
                }

                _logger.LogInformation("Successfully submitted order: {OrderTrackingId}", orderResponse.OrderTrackingId);
                return ServiceResult<PesaOrderResponse>.Success(orderResponse);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error submitting order");
                return ServiceResult<PesaOrderResponse>.Failure(
                    new ServerErrorException("Network error connecting to PesaPal"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting order to PesaPal");
                return ServiceResult<PesaOrderResponse>.Failure(
                    new ServerErrorException("An unexpected error occurred while submitting order"));
            }
        }

        public async Task<ServiceResult<PesaOrderResponse>> InitiatePesaPalPaymentAsync(InitiatePesaPalDto initiatePesaPalDto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var jsonparams = JsonSerializer.Serialize(initiatePesaPalDto, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        Console.WriteLine(jsonparams);
                        // Validate input parameters
                        if (string.IsNullOrWhiteSpace(initiatePesaPalDto.ProductId))
                        {
                            return ServiceResult<PesaOrderResponse>.Failure(
                                new BadRequestException("Product ID is required"));
                        }

                        if (string.IsNullOrWhiteSpace(initiatePesaPalDto.CustomerId))
                        {
                            return ServiceResult<PesaOrderResponse>.Failure(
                                new BadRequestException("Customer ID is required"));
                        }

                        if (initiatePesaPalDto.Amount <= 0)
                        {
                            return ServiceResult<PesaOrderResponse>.Failure(
                                new BadRequestException("Amount must be greater than zero"));
                        }

                        if (initiatePesaPalDto.BillingAddress == null)
                        {
                            return ServiceResult<PesaOrderResponse>.Failure(
                                new BadRequestException("Billing address is required"));
                        }

                        if (string.IsNullOrWhiteSpace(initiatePesaPalDto.CallbackUrl))
                        {
                            return ServiceResult<PesaOrderResponse>.Failure(
                                new BadRequestException("Callback URL is required"));
                        }

                        _logger.LogInformation("Initiating PesaPal payment for product {ProductId}, amount {Amount}",
                            initiatePesaPalDto.ProductId, initiatePesaPalDto.Amount);

                        // Step 1: Authenticate with PesaPal
                        var authResult = await AuthenticateAsync();
                        if (!authResult.IsSuccess || authResult.Data == null)
                        {
                            _logger.LogError("PesaPal authentication failed");
                            return ServiceResult<PesaOrderResponse>.Failure(
                                authResult.Error ?? new UnAuthorizedException("Authentication failed"));
                        }

                        _logger.LogInformation("PesaPal authentication successful");

                        // Step 2: Get or register IPN
                        string notificationId;
                        var ipnListResult = await GetRegisteredIPNsAsync();

                        if (ipnListResult.IsSuccess && ipnListResult.Data?.Any() == true)
                        {
                            // Use the first active IPN
                            var activeIpn = ipnListResult.Data.FirstOrDefault(i => i.IpnStatus == 1); // 1 = Active
                            if (activeIpn != null)
                            {
                                notificationId = activeIpn.IpnId;
                                _logger.LogInformation("Using existing IPN: {IpnId}", notificationId);
                            }
                            else
                            {
                                // No active IPN found, register a new one
                                var registerResult = await RegisterNewIPN(initiatePesaPalDto.IpnCallbackUrl);
                                if (!registerResult.IsSuccess)
                                {
                                    return ServiceResult<PesaOrderResponse>.Failure(registerResult.Error);
                                }
                                notificationId = registerResult.Data;
                            }
                        }
                        else
                        {
                            // No IPNs registered, create a new one
                            var registerResult = await RegisterNewIPN(initiatePesaPalDto.IpnCallbackUrl);
                            if (!registerResult.IsSuccess)
                            {
                                return ServiceResult<PesaOrderResponse>.Failure(registerResult.Error);
                            }
                            notificationId = registerResult.Data;
                        }

                        // Step 3: Create order request
                        var order = new Order
                        {
                            Id = Guid.NewGuid().ToString(),
                            TotalAmout = initiatePesaPalDto.Amount,
                            Status = OrderStatus.PENDING,
                            OrderDate = DateTimeOffset.UtcNow,
                            CustomerId = initiatePesaPalDto.CustomerId,
                            OrderNote = initiatePesaPalDto.Description
                        };

                        await _context.Orders.AddAsync(order);
                        await _context.SaveChangesAsync();

                        var orderDetail = new OrderDetail
                        {
                            Id = Guid.NewGuid().ToString(),
                            ProductId = initiatePesaPalDto.ProductId,
                            OrderId = order.Id,
                            TotalPrice = initiatePesaPalDto.Amount,
                            Quantity = 1, 
                            DetailNote = "PesaPal payment initiation",                            
                        };
                        await _context.OrderDetails.AddAsync(orderDetail);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Created order {OrderId} for customer {CustomerId} with total amount {Amount}",
                            order.Id, order.CustomerId, initiatePesaPalDto.Amount);

                        var orderRequest = new PesaOrderRequest
                        {
                            Id = initiatePesaPalDto.ProductId,
                            Currency = initiatePesaPalDto.Currency,
                            Amount = initiatePesaPalDto.Amount,
                            Description = initiatePesaPalDto.Description,
                            CallbackUrl = initiatePesaPalDto.CallbackUrl,
                            NotificationId = notificationId,
                            BillingAddress = initiatePesaPalDto.BillingAddress,
                            AccountNumber = initiatePesaPalDto.CustomerId,
                            SubscriptionDetails = initiatePesaPalDto.SubscriptionDetails
                        };
                        var json = JsonSerializer.Serialize(orderRequest, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        Console.WriteLine(json);

                        // Step 4: Submit order to PesaPal
                        var submitResult = await SubmitOrderAsync(orderRequest);

                        if (!submitResult.IsSuccess || submitResult.Data == null)
                        {
                            // Update order status to failed
                            order.Status = OrderStatus.FAILED;
                            _context.Orders.Update(order);
                            await _context.SaveChangesAsync();

                            _logger.LogError("Order submission to PesaPal failed for order {OrderId}", order.Id);

                            // Rollback transaction
                            await transaction.RollbackAsync();

                            return ServiceResult<PesaOrderResponse>.Failure(
                                submitResult.Error ?? new ServerErrorException("Failed to submit order to PesaPal"));
                        }

                        var json1 = JsonSerializer.Serialize(submitResult.Data, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        Console.WriteLine(json1);

                        var payment = new Payment
                        {
                            Id = Guid.NewGuid().ToString(),
                            OrderId = order.Id,
                            OrderTrackingId = submitResult.Data.OrderTrackingId,
                            MerchantReference = order.Id,
                            Amount = initiatePesaPalDto.Amount,
                            Currency = initiatePesaPalDto.Currency,
                            Status = PaymentStatus.PENDING,
                            Description = orderRequest.Description,
                        };

                        await _context.Payments.AddAsync(payment);
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();

                        _logger.LogInformation(
                            "PesaPal payment initiated successfully. Order tracking ID: {OrderTrackingId}",
                            submitResult.Data.OrderTrackingId);

                        return ServiceResult<PesaOrderResponse>.Success(submitResult.Data);
                    }
                    catch (HttpRequestException ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Network error during PesaPal payment initiation");
                        return ServiceResult<PesaOrderResponse>.Failure(
                            new ServerErrorException("Network error connecting to PesaPal"));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error initiating PesaPal payment for product {ProductId}", initiatePesaPalDto.ProductId);
                        return ServiceResult<PesaOrderResponse>.Failure(
                            new ServerErrorException("An unexpected error occurred while initiating payment"));
                    }
                }
            });
        }

        private async Task<ServiceResult<string>> RegisterNewIPN(string? ipnCallbackUrl)
        {
            try
            {
                // Use provided IPN URL or construct a default one
                var ipnUrl = ipnCallbackUrl ?? $"{ApiBaseUrl}/api/PesaPal/ipn-callback";

                _logger.LogInformation("Registering new IPN with URL: {IpnUrl}", ipnUrl);

                var registerResult = await RegisterIPNAsync(ipnUrl, "GET");

                if (!registerResult.IsSuccess || registerResult.Data == null)
                {
                    _logger.LogError("Failed to register IPN URL: {IpnUrl}", ipnUrl);
                    return ServiceResult<string>.Failure(
                        registerResult.Error ?? new ServerErrorException("Failed to register IPN"));
                }

                _logger.LogInformation("IPN registered successfully: {IpnId}", registerResult.Data.IpnId);
                return ServiceResult<string>.Success(registerResult.Data.IpnId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering new IPN");
                return ServiceResult<string>.Failure(
                    new ServerErrorException("Failed to register IPN"));
            }
        }
        public async Task<ServiceResult<TransactionStatusResponse>> GetTransactionStatusAsync(string orderTrackingId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderTrackingId))
                {
                    return ServiceResult<TransactionStatusResponse>.Failure(
                        new BadRequestException("Order tracking ID is required"));
                }

                var authResult = await AuthenticateAsync();
                if (!authResult.IsSuccess || authResult.Data == null)
                {
                    return ServiceResult<TransactionStatusResponse>.Failure(
                        authResult.Error ?? new UnAuthorizedException("Authentication failed"));
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Data.Token);

                var response = await client.GetAsync($"{ApiBaseUrl}/api/Transactions/GetTransactionStatus?orderTrackingId={orderTrackingId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Transaction status check failed: {StatusCode} - {Response}", response.StatusCode, responseContent);

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return ServiceResult<TransactionStatusResponse>.Failure(
                            new NotFoundException($"Transaction with tracking ID '{orderTrackingId}' not found"));
                    }

                    return ServiceResult<TransactionStatusResponse>.Failure(
                        new ServerErrorException($"Failed to get transaction status: {response.StatusCode}"));
                }

                var statusResponse = JsonSerializer.Deserialize<TransactionStatusResponse>(responseContent);

                if (statusResponse == null)
                {
                    return ServiceResult<TransactionStatusResponse>.Failure(
                        new ServerErrorException("Invalid response from PesaPal"));
                }

                _logger.LogInformation("Transaction status for {OrderTrackingId}: {Status}",
                    orderTrackingId, statusResponse.PaymentStatusDescription);

                return ServiceResult<TransactionStatusResponse>.Success(statusResponse);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error getting transaction status");
                return ServiceResult<TransactionStatusResponse>.Failure(
                    new ServerErrorException("Network error connecting to PesaPal"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction status");
                return ServiceResult<TransactionStatusResponse>.Failure(
                    new ServerErrorException("An unexpected error occurred while checking transaction status"));
            }
        }

        public async Task<ServiceResult<(OrderDto order, PesaOrderRequest request)>> CreateOrderAsync(
            string customerId,
            List<OrderDetailDto> orderDetails,
            BillingAddress billingAddress,
            string callbackUrl,
            string notificationId,
            SubscriptionDetails? subscriptionDetails = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return ServiceResult<(OrderDto, PesaOrderRequest)>.Failure(
                        new BadRequestException("Customer ID is required"));
                }

                if (orderDetails == null || !orderDetails.Any())
                {
                    return ServiceResult<(OrderDto, PesaOrderRequest)>.Failure(
                        new BadRequestException("Order must contain at least one item"));
                }

                if (billingAddress == null)
                {
                    return ServiceResult<(OrderDto, PesaOrderRequest)>.Failure(
                        new BadRequestException("Billing address is required"));
                }

                decimal totalAmount = 0;
                foreach (var detail in orderDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product == null)
                    {
                        _logger.LogError("Product not found: {ProductId}", detail.ProductId);
                        return ServiceResult<(OrderDto, PesaOrderRequest)>.Failure(
                            new NotFoundException($"Product with ID '{detail.ProductId}' not found"));
                    }

                    if (product.Price == null || product.Price <= 0)
                    {
                        return ServiceResult<(OrderDto, PesaOrderRequest)>.Failure(
                            new BadRequestException($"Product '{product.Name}' has no valid price"));
                    }

                    detail.UnitPrice = product.Price;
                    detail.TotalPrice = detail.UnitPrice * detail.Quantity;

                    if (detail.DiscountPercent.HasValue && detail.DiscountPercent > 0)
                    {
                        detail.TotalPrice -= detail.TotalPrice * (detail.DiscountPercent.Value / 100);
                    }
                    else if (detail.DiscountValue.HasValue && detail.DiscountValue > 0)
                    {
                        detail.TotalPrice -= detail.DiscountValue.Value;
                    }

                    totalAmount += detail.TotalPrice ?? 0;
                }

                if (totalAmount <= 0)
                {
                    return ServiceResult<(OrderDto, PesaOrderRequest)>.Failure(
                        new BadRequestException("Order total amount must be greater than zero"));
                }

                var order = new Order
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = customerId,
                    TotalAmout = totalAmount,
                    Status = OrderStatus.PENDING,
                    OrderDate = DateTimeOffset.UtcNow
                };

                await _context.Orders.AddAsync(order);

                foreach (var detail in orderDetails)
                {
                    detail.Id = Guid.NewGuid().ToString();
                    detail.OrderId = order.Id;
                    var dt = detail.Adapt<OrderDetail>();
                    await _context.OrderDetails.AddAsync(dt);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var pesapalRequest = new PesaOrderRequest
                {
                    Id = order.Id,
                    Currency = "UGX",
                    Amount = totalAmount,
                    Description = $"Order payment for {orderDetails.Count} item(s)",
                    CallbackUrl = callbackUrl,
                    NotificationId = notificationId,
                    BillingAddress = billingAddress,
                    AccountNumber = customerId,
                    SubscriptionDetails = subscriptionDetails
                };

                _logger.LogInformation("Created order {OrderId} with total amount {Amount}", order.Id, totalAmount);

                return ServiceResult<(OrderDto, PesaOrderRequest)>.Success((order.Adapt<OrderDto>(), pesapalRequest));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating order");
                return ServiceResult<(OrderDto, PesaOrderRequest)>.Failure(
                    new ServerErrorException("Failed to create order"));
            }
        }

        public async Task<ServiceResult<bool>> ProcessIPNNotificationAsync(string orderTrackingId, string orderMerchantReference)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderTrackingId))
                {
                    return ServiceResult<bool>.Failure(
                        new BadRequestException("Order tracking ID is required"));
                }

                if (string.IsNullOrWhiteSpace(orderMerchantReference))
                {
                    return ServiceResult<bool>.Failure(
                        new BadRequestException("Order merchant reference is required"));
                }

                var transactionResult = await GetTransactionStatusAsync(orderTrackingId);
                if (!transactionResult.IsSuccess || transactionResult.Data == null)
                {
                    _logger.LogError("Failed to get transaction status for IPN: {OrderTrackingId}", orderTrackingId);
                    return ServiceResult<bool>.Failure(
                        transactionResult.Error ?? new ServerErrorException("Failed to verify transaction status"));
                }

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderMerchantReference);
                if (order == null)
                {
                    _logger.LogError("Order not found: {OrderMerchantReference}", orderMerchantReference);
                    return ServiceResult<bool>.Failure(
                        new NotFoundException($"Order with ID '{orderMerchantReference}' not found"));
                }

                var previousStatus = order.Status;
                order.Status = MapPaymentStatusToOrderStatus(transactionResult.Data.PaymentStatusDescription ?? "");

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated order {OrderId} status from {PreviousStatus} to {NewStatus} based on IPN",
                    order.Id,
                    previousStatus,
                    order.Status);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing IPN notification");
                return ServiceResult<bool>.Failure(
                    new ServerErrorException("Failed to process payment notification"));
            }
        }

        private OrderStatus MapPaymentStatusToOrderStatus(string paymentStatus)
        {
            return paymentStatus.ToUpperInvariant() switch
            {
                "COMPLETED" => OrderStatus.COMPLETED,
                "FAILED" => OrderStatus.FAILED,
                "REVERSED" => OrderStatus.REVERSED,
                "INVALID" => OrderStatus.INVALID,
                "CANCELLED" => OrderStatus.CANCELLED,
                _ => OrderStatus.PENDING
            };
        }

        public async Task<ServiceResult<PesaOrderResponse>> ProcessOrderPaymentAsync(
            string customerId,
            List<OrderDetailDto> orderDetails,
            BillingAddress billingAddress,
            string callbackUrl,
            string ipnId,
            SubscriptionDetails? subscriptionDetails = null)
        {
            try
            {
                var orderResult = await CreateOrderAsync(
                    customerId,
                    orderDetails,
                    billingAddress,
                    callbackUrl,
                    ipnId,
                    subscriptionDetails);

                if (!orderResult.IsSuccess || orderResult.Data.order == null)
                {
                    return ServiceResult<PesaOrderResponse>.Failure(
                        orderResult.Error ?? new ServerErrorException("Failed to create order"));
                }

                var submitResult = await SubmitOrderAsync(orderResult.Data.request);

                if (!submitResult.IsSuccess)
                {
                    var order = await _context.Orders.FindAsync(orderResult.Data.order.Id);
                    if (order != null)
                    {
                        order.Status = OrderStatus.FAILED;
                        await _context.SaveChangesAsync();
                    }

                    return ServiceResult<PesaOrderResponse>.Failure(
                        submitResult.Error ?? new ServerErrorException("Failed to submit order to PesaPal"));
                }

                return submitResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order payment");
                return ServiceResult<PesaOrderResponse>.Failure(
                    new ServerErrorException("An unexpected error occurred while processing payment"));
            }
        }
    }
}
