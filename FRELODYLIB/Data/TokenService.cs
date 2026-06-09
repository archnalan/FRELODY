using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos.AuthDtos;
using FRELODYAPP.Dtos.UserDtos;
using FRELODYAPP.Models.SubModels;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FRELODYLIB.ServiceHandler.ResultModels;
using Mapster;
using FRELODYSHRD.Dtos.AuthDtos;

namespace FRELODYAPP.Data
{
    public class TokenService : TokenServiceBase
    {
        private readonly IConfiguration _config;
        private readonly UserManager<User> _userManager;
        private readonly SongDbContext _context;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            IConfiguration config,
            UserManager<User> userManager,
            SongDbContext context,
            ILogger<TokenService> logger)
        {
            _config = config;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<LoginResponseDto> GenerateTokens(User user, string? tenantId, string? deviceId = null, string? deviceName = null)
        {
            //var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            //var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(user);
            var rolesList = roles.ToList();
            var orgRoles = rolesList
                .Where(FRELODYSHRD.Constants.UserRoles.IsOrgRole)
                .ToList();
            var platformRoles = rolesList
                .Where(FRELODYSHRD.Constants.UserRoles.IsPlatformRole)
                .ToList();

            string? organizationName = null;
            if (!string.IsNullOrEmpty(tenantId))
            {
                organizationName = await _context.Tenants
                    .IgnoreQueryFilters()
                    .Where(t => t.Id == tenantId)
                    .Select(t => t.TenantName)
                    .FirstOrDefaultAsync();
            }

            var userClaimsDto = new UserClaimsDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                UserName = user.UserName,
                Roles = rolesList,
                OrgRoles = orgRoles,
                PlatformRoles = platformRoles,
                MustChangePassword = user.MustChangePassword,
                OrganizationName = organizationName,
                TenantId = tenantId,
                UserType = user.UserType,
                BillingStatus = user.BillingStatus
            };
            userClaimsDto.TenantId = tenantId ?? user.TenantId;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim("UserId", user.Id), // Redundant but useful
                new Claim("user", System.Text.Json.JsonSerializer.Serialize(userClaimsDto)), 
                new Claim("TenantId", tenantId ?? string.Empty),
                new Claim("UserType", user.UserType.ToString()),
                new Claim("org_roles", string.Join(",", orgRoles)),
                new Claim("platform_roles", string.Join(",", platformRoles)),
                new Claim("must_change_password", user.MustChangePassword ? "true" : "false")
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            claims.Add(new Claim("TenantId", userClaimsDto.TenantId ?? string.Empty));

            if (user.UserType != null)
            {
                claims.Add(new Claim("UserType", user.UserType.ToString()!));
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            // Get token expiration from config
            int tokenExpiryDays = _config.GetValue<int>("Jwt:TokenExpirationDays", 7);
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(tokenExpiryDays),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);

            string refreshToken = GenerateRefreshToken();
            await StoreRefreshToken(user.Id, refreshToken, deviceId, deviceName);

            int otherSessions = deviceId != null
                ? await CountOtherActiveSessions(user.Id, deviceId)
                : 0;

            var result = new LoginResponseDto
            {
                Token = accessToken,
                TenantId = user.TenantId,
                RefreshToken = refreshToken,
                User = userClaimsDto,
                DeviceId = deviceId,
                OtherActiveSessionsCount = otherSessions
            };

            return result;
        }

        private async Task StoreRefreshToken(string userId, string refreshToken, string? deviceId = null, string? deviceName = null)
        {
            int refreshTokenExpiryDays = _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 30);
            var now = DateTime.UtcNow;

            UserRefreshToken? existing;
            if (deviceId != null)
            {
                // Per-device: upsert by userId + deviceId
                existing = await _context.UserRefreshTokens
                    .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceId == deviceId);
            }
            else
            {
                // Legacy: one token per user (no device tracking)
                existing = await _context.UserRefreshTokens
                    .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceId == null);
            }

            if (existing != null)
            {
                existing.Token = refreshToken;
                existing.ExpiryDate = now.AddDays(refreshTokenExpiryDays);
                existing.RevokedDate = null;
                existing.LastSeenAt = now;
                if (deviceName != null) existing.DeviceName = deviceName;
                _context.UserRefreshTokens.Update(existing);
            }
            else
            {
                await _context.UserRefreshTokens.AddAsync(new UserRefreshToken
                {
                    UserId = userId,
                    Token = refreshToken,
                    ExpiryDate = now.AddDays(refreshTokenExpiryDays),
                    DeviceId = deviceId,
                    DeviceName = deviceName,
                    LastSeenAt = now
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task<int> CountOtherActiveSessions(string userId, string currentDeviceId)
        {
            return await _context.UserRefreshTokens.CountAsync(t =>
                t.UserId == userId &&
                t.DeviceId != null &&
                t.DeviceId != currentDeviceId &&
                t.RevokedDate == null &&
                t.ExpiryDate > DateTime.UtcNow);
        }

        public async Task<List<DeviceSessionDto>> GetActiveSessions(string userId, string? currentDeviceId)
        {
            var sessions = await _context.UserRefreshTokens
                .Where(t => t.UserId == userId && t.RevokedDate == null && t.ExpiryDate > DateTime.UtcNow)
                .OrderByDescending(t => t.LastSeenAt)
                .Select(t => new DeviceSessionDto
                {
                    Id = t.Id,
                    DeviceId = t.DeviceId,
                    DeviceName = t.DeviceName ?? "Unknown device",
                    IpAddress = t.IpAddress,
                    LastSeenAt = t.LastSeenAt,
                    IsCurrentDevice = t.DeviceId != null && t.DeviceId == currentDeviceId
                })
                .ToListAsync();
            return sessions;
        }

        public async Task<ServiceResult<bool>> RevokeOtherDeviceSessions(string userId, string currentDeviceId)
        {
            try
            {
                var others = await _context.UserRefreshTokens
                    .Where(t => t.UserId == userId &&
                                t.DeviceId != currentDeviceId &&
                                t.RevokedDate == null)
                    .ToListAsync();
                foreach (var t in others)
                    t.RevokedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking other device sessions for user {UserId}", userId);
                return ServiceResult<bool>.Failure(new ServerErrorException("Could not revoke other sessions"));
            }
        }

        public async Task<ServiceResult<LoginResponseDto>> RefreshToken(string accessToken, string refreshToken)
        {
            try
            {
                // Validate the existing, expired access token
                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false, // We don't care if the token is expired here
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidAudience = _config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]))
                };

                // Extract claims from the expired token
                ClaimsPrincipal principal = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out SecurityToken securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return ServiceResult<LoginResponseDto>.Failure(new BadRequestException("Invalid token"));
                }

                // Get user ID from the token
                var userClaim = principal.Claims.FirstOrDefault(c => c.Type == "user");
                if (userClaim == null)
                {
                    return ServiceResult<LoginResponseDto>.Failure(new BadRequestException("Invalid token claims"));
                }

                var userClaimsDto = JsonConvert.DeserializeObject<UserClaimsDto>(userClaim.Value);

                // Validate refresh token from database
                var storedRefreshToken = await _context.UserRefreshTokens
                    .FirstOrDefaultAsync(t => t.UserId == userClaimsDto.Id && t.Token == refreshToken);

                if (storedRefreshToken == null)
                {
                    return ServiceResult<LoginResponseDto>.Failure(new BadRequestException("Invalid refresh token"));
                }

                if (storedRefreshToken.RevokedDate != null)
                {
                    return ServiceResult<LoginResponseDto>.Failure(new BadRequestException("Refresh token has been revoked"));
                }

                if (storedRefreshToken.ExpiryDate < DateTime.UtcNow)
                {
                    storedRefreshToken.RevokedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return ServiceResult<LoginResponseDto>.Failure(new BadRequestException("Refresh token expired"));
                }

                // Touch LastSeenAt on the stored record before rotating
                storedRefreshToken.LastSeenAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Get tenant ID from the token
                var tenantClaim = principal.Claims.FirstOrDefault(c => c.Type == "TenantId");
                if (tenantClaim == null)
                {
                    return ServiceResult<LoginResponseDto>.Failure(new BadRequestException("Invalid tenant claim"));
                }

                string tenantId;
                if (!string.IsNullOrEmpty(tenantClaim.Value))
                {
                    return ServiceResult<LoginResponseDto>.Failure(new BadRequestException("Tenant Id must be provided"));
                }
                tenantId = tenantClaim.Value;
                // Find user
                var user = await _userManager.FindByIdAsync(userClaimsDto.Id);
                if (user == null)
                {
                    return ServiceResult<LoginResponseDto>.Failure(new NotFoundException("User not found"));
                }

                // Generate new tokens, preserving the device association
                var newTokens = await GenerateTokens(user, tenantId, storedRefreshToken.DeviceId, storedRefreshToken.DeviceName);

                return ServiceResult<LoginResponseDto>.Success(newTokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return ServiceResult<LoginResponseDto>.Failure(new ServerErrorException("An error occurred while refreshing the token"));
            }
        }

        public async Task<ServiceResult<bool>> RevokeRefreshToken(string tokenValue)
        {
            try
            {
                var record = await _context.UserRefreshTokens
                    .FirstOrDefaultAsync(t => t.Token == tokenValue);

                if (record != null)
                {
                    record.RevokedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh token");
                return ServiceResult<bool>.Failure(new ServerErrorException("An error occurred while revoking the token"));
            }
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public override string GetUserIdFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // We don't care if the token is expired here
                ValidateIssuerSigningKey = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidAudience = _config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]))
            };
            // Extract claims from the expired token
            ClaimsPrincipal principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            // Get user ID from the token
            var userClaim = principal.Claims.FirstOrDefault(c => c.Type == "user");
            if (userClaim == null)
            {
                return null;
            }
            var userClaimsDto = JsonConvert.DeserializeObject<UserClaimsDto>(userClaim.Value);
            return userClaimsDto.Id;
        }
    }
}