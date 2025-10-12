using FRELODYAPP.Dtos.AuthDtos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Services
{
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly IStorageService _localStorage;
        private readonly ILogger<AuthHeaderHandler> _logger;

        public AuthHeaderHandler(IStorageService localStorage, ILogger<AuthHeaderHandler> logger)
        {
            _localStorage = localStorage;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var sessionModel = (await _localStorage.GetItemAsync<LoginResponseDto>("sessionState"));

                // Add Authorization header
                if (!string.IsNullOrEmpty(sessionModel?.Token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionModel.Token);
                }

                // Add Tenant ID header
                if (!string.IsNullOrEmpty(sessionModel?.TenantId))
                {
                    if (request.Headers.TryGetValues("TenantId", out _))
                    {
                        request.Headers.Remove("TenantId");
                    }
                    request.Headers.Add("TenantId", sessionModel.TenantId);
                }

                // Call the inner handler (continue the request)
                return await base.SendAsync(request, cancellationToken);
            }
            catch (TimeoutException ex)
            {
                _logger.LogError("Request timed out: {ex}", ex);
                throw new TimeoutException("The request has timed out. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in AuthHeaderHandler: {ex}", ex);
                throw;
            }
        }
    }
}
