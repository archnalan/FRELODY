using FRELODYUI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FRELODYUI.Web.Services
{
    public class WebClipboardService : IClipboardService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<WebClipboardService> _logger;

        public WebClipboardService(IJSRuntime jsRuntime, ILogger<WebClipboardService> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public async Task CopyToClipboardAsync(string text)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying to clipboard");
                // Fallback for older browsers
                try
                {
                    await _jsRuntime.InvokeVoidAsync("fallbackCopyToClipboard", text);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback clipboard copy also failed");
                    throw;
                }
            }
        }

        public Task<bool> IsClipboardAvailableAsync()
        {
            return Task.FromResult(true); // Web always supports clipboard
        }
    }
}
