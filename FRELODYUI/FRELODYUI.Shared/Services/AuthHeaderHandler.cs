using FRELODYAPP.Dtos.AuthDtos;
using FRELODYSHRD.Dtos.AuthDtos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Services
{
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly IStorageService _localStorage;
        private readonly ILogger<AuthHeaderHandler> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly SemaphoreSlim _refreshLock = new(1, 1);

        public AuthHeaderHandler(IStorageService localStorage, ILogger<AuthHeaderHandler> logger, IHttpClientFactory httpClientFactory)
        {
            _localStorage = localStorage;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var sessionModel = await _localStorage.GetItemAsync<LoginResponseDto>("sessionState");

                if (sessionModel != null && IsTokenExpired(sessionModel.Token))
                {
                    sessionModel = await TryRefreshTokenAsync(sessionModel);
                }

                if (!string.IsNullOrEmpty(sessionModel?.Token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionModel.Token);
                }

                var tenantId = sessionModel?.TenantId;

                // Only add TenantId header if it has a value
                if (!string.IsNullOrEmpty(tenantId))
                {
                    if (request.Headers.Contains("TenantId"))
                    {
                        request.Headers.Remove("TenantId");
                    }
                    request.Headers.Add("TenantId", tenantId);
                }
                else
                {
                    if (request.Headers.Contains("TenantId"))
                    {
                        request.Headers.Remove("TenantId");
                    }
                }

                // Add UserType header if available
                if (sessionModel?.UserType != null)
                {
                    if (request.Headers.Contains("UserType"))
                    {
                        request.Headers.Remove("UserType");
                    }
                    request.Headers.Add("UserType", sessionModel.UserType.ToString());
                }

                var response = await base.SendAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized && sessionModel != null
                    && !string.IsNullOrEmpty(sessionModel.RefreshToken))
                {
                    var refreshedSession = await TryRefreshTokenAsync(sessionModel);
                    if (refreshedSession != null && refreshedSession.Token != sessionModel.Token)
                    {
                        // Retry the request with the new token
                        var retryRequest = await CloneRequestAsync(request);
                        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedSession.Token);
                        response = await base.SendAsync(retryRequest, cancellationToken);
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in AuthHeaderHandler: {ex}", ex);
                throw;
            }
        }

        private async Task<LoginResponseDto?> TryRefreshTokenAsync(LoginResponseDto session)
        {
            if (string.IsNullOrEmpty(session.RefreshToken) || string.IsNullOrEmpty(session.Token))
                return session;

            await _refreshLock.WaitAsync();
            try
            {
                // Re-read in case another request already refreshed
                var currentSession = await _localStorage.GetItemAsync<LoginResponseDto>("sessionState");
                if (currentSession != null && currentSession.Token != session.Token)
                {
                    return currentSession;
                }

                var client = _httpClientFactory.CreateClient("TokenRefresh");
                var refreshDto = new RefreshTokenDto
                {
                    AccessToken = session.Token!,
                    RefreshToken = session.RefreshToken!
                };

                var json = JsonSerializer.Serialize(refreshDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/authorization/refresh-token", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var newSession = JsonSerializer.Deserialize<LoginResponseDto>(responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (newSession != null && !string.IsNullOrEmpty(newSession.Token))
                    {
                        await _localStorage.SetItemAsync("sessionState", newSession);
                        _logger.LogInformation("Token refreshed successfully");
                        return newSession;
                    }
                }
                else
                {
                    _logger.LogWarning("Token refresh failed with status {StatusCode}", response.StatusCode);
                    await _localStorage.RemoveItemAsync("sessionState");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
            }
            finally
            {
                _refreshLock.Release();
            }

            return null;
        }

        private static bool IsTokenExpired(string? token)
        {
            if (string.IsNullOrEmpty(token))
                return true;

            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3)
                    return true;

                var payload = parts[1];
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }
                var jsonBytes = Convert.FromBase64String(payload);
                var claims = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
                if (claims == null || !claims.TryGetValue("exp", out var expValue))
                    return true;

                if (!long.TryParse(expValue.ToString(), out var exp))
                    return true;

                var expDate = DateTimeOffset.FromUnixTimeSeconds(exp);
                // Consider expired if within 60 seconds of expiry
                return expDate <= DateTimeOffset.UtcNow.AddSeconds(60);
            }
            catch
            {
                return true;
            }
        }

        private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);
            if (request.Content != null)
            {
                var content = await request.Content.ReadAsByteArrayAsync();
                clone.Content = new ByteArrayContent(content);
                if (request.Content.Headers.ContentType != null)
                    clone.Content.Headers.ContentType = request.Content.Headers.ContentType;
            }

            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}
