using FRELODYLIB.Interfaces;
using FRELODYSHRD.Dtos;
using FRELODYUI.Shared.RefitApis;
using FRELODYUI.Shared.Services;
using Microsoft.Extensions.Logging;

namespace FRELODYUI.Services
{
    public class ShareService : IShareService
    {
        private readonly IShareApi _shareApi;
        private readonly IClipboardService _clipboardService;
        private readonly ILogger<ShareService> _logger;
        private readonly string _baseUrl;

        public ShareService(
            IShareApi shareApi,
            IClipboardService clipboardService,
            ILogger<ShareService> logger)
        {
            _shareApi = shareApi;
            _clipboardService = clipboardService;
            _logger = logger;

            // This should be configurable based on platform
#if ANDROID || IOS
            _baseUrl = "https://frelody.app"; // Production URL for mobile
#else
            _baseUrl = "https://localhost:7077"; // Development URL for web
#endif
        }

        public async Task<ShareLinkDto?> GenerateShareLinkAsync(string songId)
        {
            try
            {
                var request = new ShareLinkCreateDto { SongId = songId };
                var response = await _shareApi.GenerateShareLink(request);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    // Construct the full share URL
                    response.Content.ShareUrl = $"{_baseUrl}/shared/{response.Content.ShareToken}";
                    return response.Content;
                }

                _logger.LogWarning("Failed to generate share link for song {SongId}", songId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating share link for song {SongId}", songId);
                return null;
            }
        }

        public Task<string> GetShareUrlAsync(string shareToken)
        {
            return Task.FromResult($"{_baseUrl}/shared/{shareToken}");
        }

        public async Task NotifyLinkCopiedAsync()
        {
            // Platform-specific notification can be implemented here
            await Task.CompletedTask;
        }
    }
}
