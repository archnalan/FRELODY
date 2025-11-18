using FRELODYAPP.Data.Infrastructure;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace FRELODYAPP.Data
{
    public class SecurityUtilityService
    {
        private readonly SongDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecurityUtilityService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _cache;

        public SecurityUtilityService(
            IConfiguration configuration,
            ILogger<SecurityUtilityService> logger,
            IMemoryCache cache,
            SongDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _logger = logger;
            _cache = cache;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public bool CheckRateLimit(string key, int maxAttempts, TimeSpan period)
        {
            // Get current attempt count
            if (!_cache.TryGetValue(key, out int attempts))
            {
                attempts = 0;
            }

            // Check if rate limit exceeded
            if (attempts >= maxAttempts)
            {
                _logger.LogWarning("Rate limit exceeded for key: {Key}", key);
                return false;
            }

            // Increment attempts
            _cache.Set(key, attempts + 1, period);
            return true;
        }

        public ServiceResult<bool> CheckPasswordResetRateLimit(string emailOrIp)
        {
            int maxAttempts = _configuration.GetValue<int>("Security:MaxPasswordResetAttempts", 3);
            int periodMinutes = _configuration.GetValue<int>("Security:PasswordResetPeriodMinutes", 60);

            string key = $"pwdreset_{emailOrIp}";

            if (!CheckRateLimit(key, maxAttempts, TimeSpan.FromMinutes(periodMinutes)))
            {
                return ServiceResult<bool>.Failure(
                    new TooManyRequestsException($"Too many password reset attempts. Please try again after {periodMinutes} minutes.")
                );
            }

            return ServiceResult<bool>.Success(true);
        }

        public ServiceResult<bool> CheckLoginRateLimit(string usernameOrIp)
        {
            int maxAttempts = _configuration.GetValue<int>("Security:MaxFailedLoginAttempts", 5);
            int periodMinutes = _configuration.GetValue<int>("Security:LockoutTimeMinutes", 15);

            string key = $"login_{usernameOrIp}";

            if (!CheckRateLimit(key, maxAttempts, TimeSpan.FromMinutes(periodMinutes)))
            {
                return ServiceResult<bool>.Failure(
                    new TooManyRequestsException($"Too many failed login attempts. Account is temporarily locked for {periodMinutes} minutes.")
                );
            }

            return ServiceResult<bool>.Success(true);
        }

        public void ResetLoginAttempts(string usernameOrIp)
        {
            string key = $"login_{usernameOrIp}";
            _cache.Remove(key);
        }

        public string NormalizePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber)) return "";

            try
            {
                // Remove all non-digit characters
                var digitsOnly = string.Join("", phoneNumber.Where(char.IsDigit));

                // If it's empty after removing non-digits, return empty string
                if (string.IsNullOrEmpty(digitsOnly))
                    return "";

                return digitsOnly;
            }
            catch
            {
                return "";
            }
        }

        // Additional helper for audit logging
        public async Task LogSecurityEvent(string userId, string eventType, string description, string ipAddress = null)
        {
            try
            {
                // This would typically save to a security audit log table
                // For now, we'll just log it
                _logger.LogInformation(
                    "Security Event: {EventType} | User: {UserId} | IP: {IpAddress} | {Description}",
                    eventType, userId, ipAddress, description);

                // In a real implementation, you'd save this to a database table
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event");
            }
        }

        public async Task<ServiceResult<string>> LogUserLogin(string userId, string? deviceInfo = null)
        {
            try
            {
                var loginHistory = new UserLoginHistory
                {
                    UserId = userId,
                    LoginTime = DateTimeOffset.UtcNow,
                    DeviceInfo = deviceInfo ?? GetDeviceInfo(),
                    IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                    IsActiveSession = true
                };

                await _context.UserLoginHistories.AddAsync(loginHistory);
                await _context.SaveChangesAsync();

                // Update user's last login time
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.LastLoginDate = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return ServiceResult<string>.Success(loginHistory.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging user login for user {UserId}", userId);
                return ServiceResult<string>.Failure(new ServerErrorException("Failed to log login"));
            }
        }

        public async Task<ServiceResult<bool>> LogUserLogout(string userId)
        {
            try
            {
                var activeLogin = await _context.UserLoginHistories
                    .Where(x => x.UserId == userId && x.IsActiveSession)
                    .OrderByDescending(x => x.LoginTime)
                    .FirstOrDefaultAsync();

                if (activeLogin != null)
                {
                    activeLogin.IsActiveSession = false;
                    activeLogin.LastLogoutTime = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging user logout for user {UserId}", userId);
                return ServiceResult<bool>.Failure(new ServerErrorException("Failed to log logout"));
            }
        }

        public async Task<ServiceResult<DateTimeOffset?>> GetLastLoginTime(string userId)
        {
            try
            {
                var lastLogin = await _context.UserLoginHistories
                    .Where(x => x.UserId == userId && !x.IsActiveSession)
                    .OrderByDescending(x => x.LoginTime)
                    .Select(x => (DateTimeOffset?)x.LoginTime)
                    .FirstOrDefaultAsync();

                return ServiceResult<DateTimeOffset?>.Success(lastLogin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last login time for user {UserId}", userId);
                return ServiceResult<DateTimeOffset?>.Failure(new ServerErrorException("Failed to get last login time"));
            }
        }

        private string GetDeviceInfo()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "Unknown";

            var userAgent = context.Request.Headers["User-Agent"].ToString();
            // You can parse user agent here or use a library
            return userAgent.Length > 200 ? userAgent.Substring(0, 200) : userAgent;
        }
    }   
}