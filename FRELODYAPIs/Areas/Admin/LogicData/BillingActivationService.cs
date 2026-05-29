using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Data.Infrastructure;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using Microsoft.EntityFrameworkCore;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class BillingActivationService : IBillingActivationService
    {
        private readonly SongDbContext _context;
        private readonly ILogger<BillingActivationService> _logger;

        public BillingActivationService(SongDbContext context, ILogger<BillingActivationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ServiceResult<bool>> ActivatePremiumAsync(string userId, Product product)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult<bool>.Failure(new BadRequestException("User ID is required."));

                // Self-lookup by primary key — bypass tenant/active filters.
                var user = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user is null)
                    return ServiceResult<bool>.Failure(new NotFoundException("User not found."));

                var period = product.Period ?? BillingPeriod.monthly;
                var now = DateTimeOffset.UtcNow;

                if (period == BillingPeriod.forever)
                {
                    user.BillingStatus = BillingStatus.ActiveLifetime;
                    user.BillingExpiresAt = null;
                }
                else
                {
                    user.BillingStatus = BillingStatus.ActiveRecurring;
                    var months = period.ToMonths();
                    // Extend from the later of now or an existing unexpired grant.
                    var from = user.BillingExpiresAt is { } exp && exp > now ? exp : now;
                    user.BillingExpiresAt = from.AddMonths(months <= 0 ? 1 : months);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Activated premium for user {UserId}: {Status} until {Expiry} (product {Product}/{Period})",
                    userId, user.BillingStatus, user.BillingExpiresAt, product.Name, period);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating premium for user {UserId}", userId);
                return ServiceResult<bool>.Failure(ex);
            }
        }
    }
}
