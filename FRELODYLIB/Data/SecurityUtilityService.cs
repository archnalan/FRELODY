using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FRELODYAPP.ServiceHandler;
using System.Net;

namespace FRELODYAPP.Data
{
    public class SecurityUtilityService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecurityUtilityService> _logger;
        private readonly IMemoryCache _cache;

        public SecurityUtilityService(
            IConfiguration configuration,
            ILogger<SecurityUtilityService> logger,
            IMemoryCache cache)
        {
            _configuration = configuration;
            _logger = logger;
            _cache = cache;
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
    }   
}