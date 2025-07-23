using Microsoft.Extensions.Logging;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Services
{
    public class ApiResponseHandler : IApiResponseHandler
    {
        private readonly ILogger<ApiResponseHandler> _logger;

        public ApiResponseHandler(ILogger<ApiResponseHandler> logger)
        {
            _logger = logger;
        }

        public T? ExtractContent<T>(IApiResponse<T> response)
        {
            if (!response.IsSuccessStatusCode)
                return default;

            return response.Content;
        }

        public string GetApiErrorMessage<T>(IApiResponse<T> response)
        {
            var errorContent = response.Error?.Content;

            if (string.IsNullOrWhiteSpace(errorContent))
            {
                return "An unknown error occurred.";
            }
            _logger.LogError("API Error: {ErrorContent}", errorContent);
            try
            {
                using var jsonDoc = JsonDocument.Parse(errorContent);
                var root = jsonDoc.RootElement;

                string? message = root.TryGetProperty("message", out var msgProp)
                    ? msgProp.GetString()
                    : "An error occurred, operation not completed.";

                _logger.LogError("API Error: {Message}", message);
                return message ?? "An error occurred, operation not completed.";
            }
            catch (JsonException)
            {
                _logger.LogError("Failed to parse error response.");
                return "Error! Operation not completed";
            }
        }
    }
}
