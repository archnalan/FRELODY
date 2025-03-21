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
        Guid GetTenantId();
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

        public Guid GetTenantId()
        {
            try
            {
                var identity = _httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity;

                if (identity != null)
                {
                    var userClaims = identity.Claims;
                    var tenantIdString = userClaims.FirstOrDefault(x => x.Type.Equals("tenantId", StringComparison.OrdinalIgnoreCase))?.Value;

                    if (string.IsNullOrEmpty(tenantIdString))
                    {
                        _logger.LogWarning("TenantId is missing in the claims.");
                        return Guid.Empty;
                    }

                    if (Guid.TryParse(tenantIdString, out Guid tenantId))
                    {
                        return tenantId;
                    }
                    else
                    {
                        _logger.LogWarning("TenantId in claims is not a valid GUID: {TenantId}", tenantIdString);
                        return Guid.Empty;
                    }
                }

                _logger.LogWarning("User identity is not available.");
                return Guid.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting TenantId.");
                return Guid.Empty;
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
