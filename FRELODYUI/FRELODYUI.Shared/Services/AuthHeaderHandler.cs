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
                var sessionModel = await _localStorage.GetItemAsync<LoginResponseDto>("sessionState");

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

                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in AuthHeaderHandler: {ex}", ex);
                throw;
            }
        }
    }
}
