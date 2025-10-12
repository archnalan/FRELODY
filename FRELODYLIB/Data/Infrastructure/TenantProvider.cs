using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FRELODYAPP.Dtos.UserDtos;
using System.Security.Claims;
using System.Text.Json;

namespace FRELODYAPP.Data.Infrastructure
{
    public interface ITenantProvider
    {
        string GetUserId();
        string GetTenantId();
        UserClaimsDto GetCurrentUser();
    }

    public class TenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TenantProvider> _logger;

        public TenantProvider(IHttpContextAccessor httpContextAccessor, ILogger<TenantProvider> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public string GetTenantId()
        {
            try
            {
                var identity = _httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity;

                if (identity != null)
                {
                    var userClaims = identity.Claims;
                    var tenantIdString = userClaims.FirstOrDefault(x =>
                     x.Type.Equals("TenantId", StringComparison.OrdinalIgnoreCase) ||
                     x.Type.Equals("tenantId", StringComparison.OrdinalIgnoreCase) ||
                     x.Type.Equals("tenant_id", StringComparison.OrdinalIgnoreCase))?.Value;

                    if (string.IsNullOrEmpty(tenantIdString))
                    {
                        var userDetails = userClaims.FirstOrDefault(x => x.Type.Equals("user", StringComparison.OrdinalIgnoreCase))?.Value;
                        if (!string.IsNullOrEmpty(userDetails))
                        {
                            try
                            {
                                var userClaimsDto = JsonSerializer.Deserialize<UserClaimsDto>(userDetails);
                                if (!string.IsNullOrEmpty(userClaimsDto?.TenantId))
                                {
                                    return userClaimsDto.TenantId;
                                }
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogWarning(ex, "Failed to deserialize user claims to get TenantId");
                            }
                        }

                        _logger.LogWarning("TenantId is missing in the claims.");
                        return string.Empty;
                    }

                    return tenantIdString;
                }

                _logger.LogWarning("User identity is not available.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting TenantId.");
                return string.Empty;
            }
        }

        public string GetUserId()
        {
            return GetCurrentUser()?.Id ?? string.Empty;
        }

        public UserClaimsDto GetCurrentUser()
        {
            try
            {
                var identity = _httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity;

                if (identity != null)
                {
                    var userClaims = identity.Claims;
                    var userDetails = userClaims.FirstOrDefault(x => x.Type.Equals("user", StringComparison.OrdinalIgnoreCase))?.Value;

                    if (userDetails == null)
                    {
                        _logger.LogWarning("User details are missing in the claims.");
                        return null;
                    }

                    var userClaimsDto = JsonSerializer.Deserialize<UserClaimsDto>(userDetails);
                    return userClaimsDto;
                }

                _logger.LogWarning("User identity is not available.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting current user.");
                return null;
            }
        }
    }
}
