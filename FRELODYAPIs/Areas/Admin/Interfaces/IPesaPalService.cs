using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.PesaPalDtos;
using FRELODYSHRD.Models.PesaPal;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface IPesaPalService
    {
        Task<ServiceResult<PesaAuthResponse>> AuthenticateAsync();
        Task<ServiceResult<(OrderDto order, PesaOrderRequest request)>> CreateOrderAsync(string customerId, List<OrderDetailDto> orderDetails, BillingAddress billingAddress, string callbackUrl, string notificationId, SubscriptionDetails? subscriptionDetails = null);
        Task<ServiceResult<List<PesaIPNResponse>>> GetRegisteredIPNsAsync();
        Task<ServiceResult<TransactionStatusResponse>> GetTransactionStatusAsync(string orderTrackingId);
        Task<ServiceResult<PesaOrderResponse>> InitiatePesaPalPaymentAsync(InitiatePesaPalDto initiatePesaPalDto);
        Task<ServiceResult<bool>> ProcessIPNNotificationAsync(string orderTrackingId, string orderMerchantReference);
        Task<ServiceResult<PesaOrderResponse>> ProcessOrderPaymentAsync(string customerId, List<OrderDetailDto> orderDetails, BillingAddress billingAddress, string callbackUrl, string ipnId, SubscriptionDetails? subscriptionDetails = null);
        Task<ServiceResult<PesaIPNResponse>> RegisterIPNAsync(string ipnUrl, string notificationType = "POST");
        Task<ServiceResult<PesaOrderResponse>> SubmitOrderAsync(PesaOrderRequest orderRequest);
    }
}
