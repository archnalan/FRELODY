using Microsoft.Extensions.Logging;
using FRELODYUI.Shared.Services;
using Microsoft.Maui.ApplicationModel.DataTransfer; 
using System;
using System.Threading.Tasks;

namespace FRELODYUI.Services
{
    public class MauiClipboardService : IClipboardService
    {
        private readonly ILogger<MauiClipboardService> _logger;

        public MauiClipboardService(ILogger<MauiClipboardService> logger)
        {
            _logger = logger;
        }

        public async Task CopyToClipboardAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            try
            {
                await Clipboard.SetTextAsync(text); // Updated namespace
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying to clipboard");
                throw;
            }
        }

        public Task<bool> IsClipboardAvailableAsync()
        {
            // Clipboard API is available on all MAUI target platforms
            return Task.FromResult(true);
        }
    }
}
