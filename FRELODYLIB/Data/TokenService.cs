using AutoMapper;
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

namespace FRELODYAPP.Data
{
    public class TokenService : TokenServiceBase
    {
        private readonly IConfiguration _config;
        private readonly UserManager<User> _userManager;
        private readonly SongDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            IConfiguration config,
            UserManager<User> userManager,
            SongDbContext context,
            IMapper mapper,
            ILogger<TokenService> logger)
        {
            _config = config;
            _userManager = userManager;
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<LoginResponseDto> GenerateTokens(User user, string? tenantId)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim("user", JsonConvert.SerializeObject(_mapper.Map<UserClaimsDto>(user)))
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            claims.Add(new Claim("TenantId", tenantId.ToString()));

            // Get token expiration from config
            int tokenExpiryDays = _config.GetValue<int>("Jwt:TokenExpirationDays", 7);
            var expiryTime = DateTime.Now.AddDays(tokenExpiryDays);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiryTime,
                signingCredentials: credentials
            );

            // Create refresh token
            string refreshToken = GenerateRefreshToken();
            int refreshTokenExpiryDays = _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 30);

            // Save refresh token to database
            var userRefreshToken = new UserRefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(refreshTokenExpiryDays)
            };

            // Check if user already has a refresh token
            var existingToken = await _context.UserRefreshTokens
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (existingToken != null)
            {
                existingToken.Token = refreshToken;
                existingToken.ExpiryDate = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);
                _context.UserRefreshTokens.Update(existingToken);
            }
            else
            {
                await _context.UserRefreshTokens.AddAsync(userRefreshToken);
            }

            await _context.SaveChangesAsync();

            var result = new LoginResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiry = expiryTime,
                TenantId = user.TenantId,
                RefreshToken = refreshToken
            };

            return result;
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

                if (storedRefreshToken.ExpiryDate < DateTime.UtcNow)
                {
                    // Remove expired refresh token
                    _context.UserRefreshTokens.Remove(storedRefreshToken);
                    await _context.SaveChangesAsync();

                    return ServiceResult<LoginResponseDto>.Failure(new BadRequestException("Refresh token expired"));
                }

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

                // Generate new tokens
                var newTokens = await GenerateTokens(user, tenantId);

                return ServiceResult<LoginResponseDto>.Success(newTokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return ServiceResult<LoginResponseDto>.Failure(new ServerErrorException("An error occurred while refreshing the token"));
            }
        }

        public async Task<ServiceResult<bool>> RevokeRefreshToken(string userId)
        {
            try
            {
                var refreshToken = await _context.UserRefreshTokens
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (refreshToken != null)
                {
                    _context.UserRefreshTokens.Remove(refreshToken);
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