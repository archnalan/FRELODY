using FRELODYAPP.Dtos.AuthDtos;
using FRELODYAPP.Dtos.UserDtos;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.UserDtos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

namespace FRELODYUI.Shared.Services
{
    public class GlobalAuthStateProvider : AuthenticationStateProvider
    {
        private const string SessionStateKey = "sessionState";
        private const string AuthenticationType = "jwt";
        
        private readonly IStorageService _localStorage;
        private readonly NavigationManager _navigationManager;
        private readonly ILogger<GlobalAuthStateProvider> _logger;

        public GlobalAuthStateProvider(
            IStorageService localStorage, 
            NavigationManager navigationManager,
            ILogger<GlobalAuthStateProvider> logger)
        {
            _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
            _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var sessionResult = await GetSessionFromStorageAsync();
                if (!sessionResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to get session from storage: {Error}", sessionResult.Error?.Message);
                    return CreateAnonymousState();
                }

                if (sessionResult.Data == null)
                {
                    _logger.LogInformation("No session found in storage");
                    return CreateAnonymousState();
                }

                var identityResult = GetClaimsIdentityFromToken(sessionResult.Data.Token);
                if (!identityResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to parse token: {Error}", identityResult.Error?.Message);
                    await ClearSessionAsync();
                    return CreateAnonymousState();
                }

                var user = new ClaimsPrincipal(identityResult.Data);
                _logger.LogInformation("User authenticated successfully");
                return new AuthenticationState(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAuthenticationStateAsync");
                await ClearSessionAsync();
                return CreateAnonymousState();
            }
        }

        public async Task<ServiceResult<bool>> MarkUserAsAuthenticatedAsync(LoginResponseDto loginResponse)
        {
            try
            {
                if (loginResponse == null)
                {
                    return ServiceResult<bool>.Failure(
                        new BadRequestException("Login response cannot be null"));
                }

                if (string.IsNullOrEmpty(loginResponse.Token))
                {
                    return ServiceResult<bool>.Failure(
                        new BadRequestException("Token is required"));
                }

                var saveResult = await SaveSessionToStorageAsync(loginResponse);
                if (!saveResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(saveResult.Error);
                }

                var identityResult = GetClaimsIdentityFromToken(loginResponse.Token);
                if (!identityResult.IsSuccess)
                {
                    await ClearSessionAsync();
                    return ServiceResult<bool>.Failure(identityResult.Error);
                }

                var user = new ClaimsPrincipal(identityResult.Data);
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
                
                _logger.LogInformation("User marked as authenticated successfully");
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MarkUserAsAuthenticated");
                await ClearSessionAsync();
                return ServiceResult<bool>.Failure(new Exception("Could not mark user as authenticated"));
            }
        }

        public async Task<ServiceResult<UserClaimsDto>> GetAuthenticatedUserAsync()
        {
            try
            {
                var sessionResult = await GetSessionFromStorageAsync();
                if (!sessionResult.IsSuccess || sessionResult.Data == null)
                {
                    return ServiceResult<UserClaimsDto>.Failure(
                        new UnAuthorizedException("No authenticated session found"));
                }

                var userClaimsResult = ExtractUserClaimsFromToken(sessionResult.Data.Token);
                if (!userClaimsResult.IsSuccess)
                {
                    return ServiceResult<UserClaimsDto>.Failure(userClaimsResult.Error);
                }

                return ServiceResult<UserClaimsDto>.Success(userClaimsResult.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAuthenticatedUser");
                return ServiceResult<UserClaimsDto>.Failure(new Exception("Could not get authenticated user"));
            }
        }

        public async Task<ServiceResult<ClaimsPrincipal>> GetUserClaimsFromTokenAsync(LoginResponseDto loginResponse)
        {
            try
            {
                if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
                {
                    return ServiceResult<ClaimsPrincipal>.Failure(
                        new BadRequestException("Valid login response with token is required"));
                }

                var identityResult = GetClaimsIdentityFromToken(loginResponse.Token);
                if (!identityResult.IsSuccess)
                {
                    return ServiceResult<ClaimsPrincipal>.Failure(identityResult.Error);
                }

                var user = new ClaimsPrincipal(identityResult.Data);
                return ServiceResult<ClaimsPrincipal>.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserClaimsFromToken");
                return ServiceResult<ClaimsPrincipal>.Failure(new Exception("Could not get user claims from token"));
            }
        }

        public async Task<ServiceResult<bool>> MarkUserAsLoggedOutAsync()
        {
            try
            {
                await ClearSessionAsync();
                
                var identity = new ClaimsIdentity();
                var user = new ClaimsPrincipal(identity);
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
                
                _logger.LogInformation("User logged out successfully");
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MarkUserAsLoggedOut");
                return ServiceResult<bool>.Failure(new Exception("Could not log out user"));
            }
        }

        public async Task<ServiceResult<bool>> IsPremiumUser()
        {
            try
            {
                var user = await GetAuthenticatedUserAsync();
                if (user.IsSuccess && user.Data != null)
                {
                    var billingStatus = user.Data.BillingStatus;
                    return ServiceResult<bool>.Success(billingStatus == BillingStatus.PremiumTrial
                    || billingStatus == BillingStatus.ActiveLifetime
                    || billingStatus == BillingStatus.ActiveRecurring);
                }
                return ServiceResult<bool>.Success(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsPremiumUser");
                return ServiceResult<bool>.Failure(new Exception("Could not determine premium status"));
            }
        }

        #region Private Helper Methods

        public async Task<UserClaimsDto?> GetLoggedInUserAsync()
        {
            var authState = await GetAuthenticationStateAsync();
            var user = authState.User;
            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                var userClaimsDto = new UserClaimsDto
                {
                    Id = user.FindFirst("UserId")?.Value,
                    FirstName = user.FindFirst("FirstName")?.Value,
                    LastName = user.FindFirst("LastName")?.Value,
                    Email = user.FindFirst("Email")?.Value,
                    UserName = user.FindFirst("UserName")?.Value,
                    UserType = Enum.TryParse(user.FindFirst("UserType")?.Value, out UserType userType) ? userType : null,
                    Roles = user.Claims
                                .Where(c => c.Type == ClaimTypes.Role)
                                .Select(c => c.Value)
                                .ToList()
                };
                return userClaimsDto;
            }
            return null;
        }
        private async Task<ServiceResult<LoginResponseDto>> GetSessionFromStorageAsync()
        {
            try
            {
                var sessionModel = await _localStorage.GetItemAsync<LoginResponseDto>(SessionStateKey);
                return ServiceResult<LoginResponseDto>.Success(sessionModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading session from storage");
                return ServiceResult<LoginResponseDto>.Failure(new Exception("Could not read session from storage"));
            }
        }

        private async Task<ServiceResult<bool>> SaveSessionToStorageAsync(LoginResponseDto loginResponse)
        {
            try
            {
                await _localStorage.SetItemAsync(SessionStateKey, loginResponse);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving session to storage");
                return ServiceResult<bool>.Failure(new Exception("Could not save session to storage"));
            }
        }

        private async Task ClearSessionAsync()
        {
            try
            {
                await _localStorage.RemoveItemAsync(SessionStateKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing session from storage");
            }
        }

        private ServiceResult<ClaimsIdentity> GetClaimsIdentityFromToken(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return ServiceResult<ClaimsIdentity>.Failure(
                        new BadRequestException("Token cannot be null or empty"));
                }

                var claimsResult = ParseClaimsFromJwt(token);
                if (!claimsResult.IsSuccess)
                {
                    return ServiceResult<ClaimsIdentity>.Failure(claimsResult.Error);
                }

                var identityResult = CreateClaimsIdentity(claimsResult.Data);
                return identityResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting claims identity from token");
                return ServiceResult<ClaimsIdentity>.Failure(new Exception("Could not get claims identity from token"));
            }
        }

        private ServiceResult<UserClaimsDto> ExtractUserClaimsFromToken(string token)
        {
            try
            {
                var claimsResult = ParseClaimsFromJwt(token);
                if (!claimsResult.IsSuccess)
                {
                    return ServiceResult<UserClaimsDto>.Failure(claimsResult.Error);
                }

                var userClaim = claimsResult.Data.FirstOrDefault(c =>
                    c.Type.Equals("user", StringComparison.OrdinalIgnoreCase));

                if (userClaim == null || string.IsNullOrEmpty(userClaim.Value))
                {
                    return ServiceResult<UserClaimsDto>.Failure(
                        new BadRequestException("User claim not found in token"));
                }

                var userClaimsDto = JsonSerializer.Deserialize<UserClaimsDto>(userClaim.Value);
                if (userClaimsDto == null)
                {
                    return ServiceResult<UserClaimsDto>.Failure(
                        new BadRequestException("Failed to deserialize user claims"));
                }

                return ServiceResult<UserClaimsDto>.Success(userClaimsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting user claims from token");
                return ServiceResult<UserClaimsDto>.Failure(new Exception("Could not extract user claims from token"));
            }
        }

        private ServiceResult<IEnumerable<Claim>> ParseClaimsFromJwt(string jwt)
        {
            try
            {
                if (string.IsNullOrEmpty(jwt))
                {
                    return ServiceResult<IEnumerable<Claim>>.Failure(
                        new BadRequestException("JWT token cannot be null or empty"));
                }

                var jwtParts = jwt.Split('.');
                if (jwtParts.Length != 3)
                {
                    return ServiceResult<IEnumerable<Claim>>.Failure(
                        new BadRequestException("Invalid JWT token format"));
                }

                var payload = jwtParts[1];
                var jsonBytes = ParseBase64WithoutPadding(payload);
                var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

                if (keyValuePairs == null)
                {
                    return ServiceResult<IEnumerable<Claim>>.Failure(
                        new BadRequestException("Failed to parse JWT payload"));
                }

                var claims = keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty));
                return ServiceResult<IEnumerable<Claim>>.Success(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing claims from JWT");
                return ServiceResult<IEnumerable<Claim>>.Failure(new Exception("Could not parse claims from JWT"));
            }
        }

        private ServiceResult<ClaimsIdentity> CreateClaimsIdentity(IEnumerable<Claim> claimsIn)
        {
            try
            {
                var claims = new List<Claim>();

                // Find and handle the "roles" claim correctly
                var rolesClaim = claimsIn.FirstOrDefault(c => 
                    c.Type == ClaimTypes.Role || c.Type.Equals("roles", StringComparison.OrdinalIgnoreCase));

                if (rolesClaim != null && !string.IsNullOrEmpty(rolesClaim.Value))
                {
                    // Check if roles are in JSON array format
                    if (rolesClaim.Value.StartsWith("[") && rolesClaim.Value.EndsWith("]"))
                    {
                        try
                        {
                            var roles = JsonSerializer.Deserialize<string[]>(rolesClaim.Value);
                            if (roles != null)
                            {
                                foreach (var role in roles)
                                {
                                    if (!string.IsNullOrEmpty(role))
                                    {
                                        claims.Add(new Claim(ClaimTypes.Role, role));
                                    }
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse roles array from token");
                            // Add the raw role claim as fallback
                            claims.Add(new Claim(ClaimTypes.Role, rolesClaim.Value));
                        }
                    }
                    else
                    {
                        // Single role or comma-separated roles
                        claims.Add(new Claim(ClaimTypes.Role, rolesClaim.Value));
                    }
                }

                // Add other claims excluding the original roles claim to avoid duplication
                var otherClaims = claimsIn.Where(c => 
                    !c.Type.Equals("roles", StringComparison.OrdinalIgnoreCase) && 
                    c.Type != ClaimTypes.Role);
                claims.AddRange(otherClaims);

                var userClaim = claimsIn.FirstOrDefault(c => c.Type == "user");
                if (userClaim != null)
                {
                    try
                    {
                        var userDto = JsonSerializer.Deserialize<UserClaimsDto>(userClaim.Value);
                        if (userDto != null)
                        {
                            claims.Add(new Claim("FirstName", userDto.FirstName ?? ""));
                            claims.Add(new Claim("LastName", userDto.LastName ?? ""));
                            claims.Add(new Claim("UserId", userDto.Id ?? ""));
                            claims.Add(new Claim("Email", userDto.Email ?? ""));
                            claims.Add(new Claim("UserName", userDto.UserName ?? ""));
                            if (userDto.UserType != null)
                            {
                                claims.Add(new Claim("UserType", userDto.UserType.ToString()!));
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize user claim");
                    }
                }

                return ServiceResult<ClaimsIdentity>.Success(new ClaimsIdentity(claims, AuthenticationType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claims identity");
                return ServiceResult<ClaimsIdentity>.Failure(new Exception("Could not create claims identity"));
            }
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }

        private static AuthenticationState CreateAnonymousState()
        {
            var identity = new ClaimsIdentity();
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }

        #endregion
    }
}
