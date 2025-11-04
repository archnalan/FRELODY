using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Models.PesaPal;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class PaymentService : IPaymentService
    {
        private readonly SongDbContext _context;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(ILogger<PaymentService> logger, SongDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<ServiceResult<List<PaymentDto>>> GetPaymentsAsync()
        {
            try
            {
                var payments = await _context.Payments
                    .Include(p => p.Order)
                    .OrderByDescending(p => p.CreatedDate)
                    .ToListAsync();

                var paymentsDto = payments.Adapt<List<PaymentDto>>();
                return ServiceResult<List<PaymentDto>>.Success(paymentsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching payments.");
                return ServiceResult<List<PaymentDto>>.Failure(
                    new ServerErrorException("An error occurred while fetching payments."));
            }
        }

        public async Task<ServiceResult<PaymentDto>> GetPaymentByIdAsync(string paymentId)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null)
                {
                    return ServiceResult<PaymentDto>.Failure(
                        new NotFoundException($"Payment with ID '{paymentId}' not found."));
                }

                var paymentDto = payment.Adapt<PaymentDto>();
                return ServiceResult<PaymentDto>.Success(paymentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching payment with ID {paymentId}.");
                return ServiceResult<PaymentDto>.Failure(
                    new ServerErrorException("An error occurred while fetching the payment."));
            }
        }

        public async Task<ServiceResult<PaymentDto>> GetPaymentByOrderIdAsync(string orderId)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.OrderId == orderId);

                if (payment == null)
                {
                    return ServiceResult<PaymentDto>.Failure(
                        new NotFoundException($"Payment for order '{orderId}' not found."));
                }

                var paymentDto = payment.Adapt<PaymentDto>();
                return ServiceResult<PaymentDto>.Success(paymentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching payment for order {orderId}.");
                return ServiceResult<PaymentDto>.Failure(
                    new ServerErrorException("An error occurred while fetching the payment."));
            }
        }

        public async Task<ServiceResult<PaymentDto>> GetPaymentByTrackingIdAsync(string trackingId)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.OrderTrackingId == trackingId);

                if (payment == null)
                {
                    return ServiceResult<PaymentDto>.Failure(
                        new NotFoundException($"Payment with tracking ID '{trackingId}' not found."));
                }

                var paymentDto = payment.Adapt<PaymentDto>();
                return ServiceResult<PaymentDto>.Success(paymentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching payment with tracking ID {trackingId}.");
                return ServiceResult<PaymentDto>.Failure(
                    new ServerErrorException("An error occurred while fetching the payment."));
            }
        }

        public async Task<ServiceResult<List<PaymentDto>>> GetPaymentsByCustomerIdAsync(string customerId)
        {
            try
            {
                var payments = await _context.Payments
                    .Include(p => p.Order)
                    .Where(p => p.Order!.CustomerId == customerId)
                    .OrderByDescending(p => p.CreatedDate)
                    .ToListAsync();

                var paymentsDto = payments.Adapt<List<PaymentDto>>();
                return ServiceResult<List<PaymentDto>>.Success(paymentsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching payments for customer {customerId}.");
                return ServiceResult<List<PaymentDto>>.Failure(
                    new ServerErrorException("An error occurred while fetching payments."));
            }
        }

        public async Task<ServiceResult<List<PaymentDto>>> GetPaymentsByStatusAsync(PaymentStatus status)
        {
            try
            {
                var payments = await _context.Payments
                    .Include(p => p.Order)
                    .Where(p => p.Status == status)
                    .OrderByDescending(p => p.CreatedDate)
                    .ToListAsync();

                var paymentsDto = payments.Adapt<List<PaymentDto>>();
                return ServiceResult<List<PaymentDto>>.Success(paymentsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching payments with status {status}.");
                return ServiceResult<List<PaymentDto>>.Failure(
                    new ServerErrorException("An error occurred while fetching payments."));
            }
        }

        public async Task<ServiceResult<PaymentDto>> AddPayment(PesaPayment payment)
        {
            try
            {
                // Validate order exists
                var orderExists = await _context.Orders.AnyAsync(o => o.Id == payment.OrderId);
                if (!orderExists)
                {
                    return ServiceResult<PaymentDto>.Failure(
                        new NotFoundException($"Order with ID '{payment.OrderId}' not found."));
                }

                // Check for duplicate tracking ID
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderTrackingId == payment.OrderTrackingId);

                if (existingPayment != null)
                {
                    return ServiceResult<PaymentDto>.Failure(
                        new BadRequestException($"Payment with tracking ID '{payment.OrderTrackingId}' already exists."));
                }

                var paymentEntity = payment.Adapt<Payment>();
                paymentEntity.Id = Guid.NewGuid().ToString();
                paymentEntity.CreatedDate = DateTimeOffset.UtcNow;

                _context.Payments.Add(paymentEntity);
                await _context.SaveChangesAsync();

                var paymentDto = paymentEntity.Adapt<PaymentDto>();
                _logger.LogInformation("Payment created successfully: {PaymentId}", paymentEntity.Id);

                return ServiceResult<PaymentDto>.Success(paymentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding a new payment.");
                return ServiceResult<PaymentDto>.Failure(
                    new ServerErrorException("An error occurred while adding the payment."));
            }
        }

        public async Task<ServiceResult<PaymentDto>> UpdatePayment(PesaPayment payment)
        {
            try
            {
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.Id == payment.Id);

                if (existingPayment == null)
                {
                    return ServiceResult<PaymentDto>.Failure(
                        new NotFoundException($"Payment with ID '{payment.Id}' not found."));
                }

                // Update only specific fields (not ID, OrderId, CreatedDate)
                existingPayment.Status = payment.Status;
                existingPayment.PaymentMethod = payment.PaymentMethod;
                existingPayment.ConfirmationCode = payment.ConfirmationCode;
                existingPayment.PaymentAccount = payment.PaymentAccount;
                existingPayment.Description = payment.Description;
                existingPayment.Message = payment.Message;
                existingPayment.Amount = payment.Amount;
                existingPayment.Currency = payment.Currency;

                if (payment.Status != PaymentStatus.PENDING && !existingPayment.CompletedDate.HasValue)
                {
                    existingPayment.CompletedDate = DateTimeOffset.UtcNow;
                }

                await _context.SaveChangesAsync();

                var paymentDto = existingPayment.Adapt<PaymentDto>();
                _logger.LogInformation("Payment updated successfully: {PaymentId}", existingPayment.Id);

                return ServiceResult<PaymentDto>.Success(paymentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating the payment.");
                return ServiceResult<PaymentDto>.Failure(
                    new ServerErrorException("An error occurred while updating the payment."));
            }
        }

        public async Task<ServiceResult<bool>> DeletePayment(string paymentId)
        {
            try
            {
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (existingPayment == null)
                {
                    return ServiceResult<bool>.Failure(
                        new NotFoundException($"Payment with ID '{paymentId}' not found."));
                }

                _context.Payments.Remove(existingPayment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment deleted successfully: {PaymentId}", paymentId);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting the payment.");
                return ServiceResult<bool>.Failure(
                    new ServerErrorException("An error occurred while deleting the payment."));
            }
        }
    }
}
