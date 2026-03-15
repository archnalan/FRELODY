using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Models.PesaPal;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface IPaymentService
    {
        Task<ServiceResult<PaymentDto>> AddPayment(PesaPayment payment);
        Task<ServiceResult<bool>> DeletePayment(string paymentId);
        Task<ServiceResult<PaymentDto>> GetPaymentByIdAsync(string paymentId);
        Task<ServiceResult<PaymentDto>> GetPaymentByOrderIdAsync(string orderId);
        Task<ServiceResult<PaymentDto>> GetPaymentByTrackingIdAsync(string trackingId);
        Task<ServiceResult<List<PaymentDto>>> GetPaymentsAsync();
        Task<ServiceResult<List<PaymentDto>>> GetPaymentsByCustomerIdAsync(string customerId);
        Task<ServiceResult<List<PaymentDto>>> GetPaymentsByStatusAsync(PaymentStatus status);
        Task<ServiceResult<PaymentDto>> UpdatePayment(PesaPayment payment);
    }
}