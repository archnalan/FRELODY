using Refit;
using FRELODYSHRD.Models.PesaPal;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IPesaPalApi
    {
        [Post("/api/pesa-pal/authenticate")]
        Task<IApiResponse<PesaAuthResponse>> Authenticate();

        [Post("/api/pesa-pal/register-ipn")]
        Task<IApiResponse<PesaIPNResponse>> RegisterIPN([Body] RegisterIPNRequest request);

        [Get("/api/pesa-pal/ipn-list")]
        Task<IApiResponse<List<PesaIPNResponse>>> GetIPNList();

        [Post("/api/pesa-pal/submit-order")]
        Task<IApiResponse<PesaOrderResponse>> SubmitOrder([Body] PesaOrderRequest orderRequest);

        [Get("/api/pesa-pal/transaction-status/{orderTrackingId}")]
        Task<IApiResponse<TransactionStatusResponse>> GetTransactionStatus(string orderTrackingId);

        [Post("/api/pesa-pal/ipn-callback")]
        Task<IApiResponse<object>> IPNCallbackPost(
            [Query] string OrderTrackingId,
            [Query] string OrderMerchantReference,
            [Query] string OrderNotificationType);

        [Get("/api/pesa-pal/ipn-callback")]
        Task<IApiResponse<object>> IPNCallbackGet(
            [Query] string OrderTrackingId,
            [Query] string OrderMerchantReference,
            [Query] string OrderNotificationType);

        [Get("/api/pesa-pal/payment-callback")]
        Task<IApiResponse> PaymentCallback(
            [Query] string OrderTrackingId,
            [Query] string OrderMerchantReference);

        [Post("/api/pesa-pal/process-payment")]
        Task<IApiResponse<PesaOrderResponse>> ProcessPayment([Body] ProcessPaymentRequest request);

        [Post("/api/pesa-pal/initiate-payment")]
        Task<IApiResponse<PesaOrderResponse>> InitiatePesaPalPayment([Body] InitiatePesaPalDto request);
    }
}