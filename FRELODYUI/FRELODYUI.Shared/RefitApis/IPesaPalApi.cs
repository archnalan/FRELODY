using Refit;
using FRELODYSHRD.Models.PesaPal;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IPesaPalApi
    {
        [Post("/api/PesaPal/authenticate")]
        Task<IApiResponse<PesaAuthResponse>> Authenticate();

        [Post("/api/PesaPal/register-ipn")]
        Task<IApiResponse<PesaIPNResponse>> RegisterIPN([Body] RegisterIPNRequest request);

        [Get("/api/PesaPal/ipn-list")]
        Task<IApiResponse<List<PesaIPNResponse>>> GetIPNList();

        [Post("/api/PesaPal/submit-order")]
        Task<IApiResponse<PesaOrderResponse>> SubmitOrder([Body] PesaOrderRequest orderRequest);

        [Get("/api/PesaPal/transaction-status/{orderTrackingId}")]
        Task<IApiResponse<TransactionStatusResponse>> GetTransactionStatus(string orderTrackingId);

        [Post("/api/PesaPal/ipn-callback")]
        Task<IApiResponse<object>> IPNCallbackPost(
            [Query] string OrderTrackingId,
            [Query] string OrderMerchantReference,
            [Query] string OrderNotificationType);

        [Get("/api/PesaPal/ipn-callback")]
        Task<IApiResponse<object>> IPNCallbackGet(
            [Query] string OrderTrackingId,
            [Query] string OrderMerchantReference,
            [Query] string OrderNotificationType);

        [Get("/api/PesaPal/payment-callback")]
        Task<IApiResponse> PaymentCallback(
            [Query] string OrderTrackingId,
            [Query] string OrderMerchantReference);

        [Post("/api/PesaPal/process-payment")]
        Task<IApiResponse<PesaOrderResponse>> ProcessPayment([Body] ProcessPaymentRequest request);
    }
}