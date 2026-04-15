using FRELODYLIB.Interfaces;
using FRELODYSHRD.Dtos;
using FRELODYUI.Shared.RefitApis;
using FRELODYUI.Shared.Services;
using Microsoft.AspNetCore.Components;
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
            ILogger<ShareService> logger,
            NavigationManager navigationManager)
        {
            _shareApi = shareApi;
            _clipboardService = clipboardService;
            _logger = logger;

#if ANDROID || IOS
            _baseUrl = "https://frelody.app";
#else
            _baseUrl = navigationManager.BaseUri.TrimEnd('/');
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
                    response.Content.ShareUrl = $"{_baseUrl}/songs/{response.Content.ShareToken}";
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

        public async Task<ShareLinkDto?> GeneratePlaylistShareLinkAsync(string playlistId)
        {
            try
            {
                var request = new ShareLinkCreateDto { PlaylistId = playlistId };
                var response = await _shareApi.GenerateShareLink(request);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    response.Content.ShareUrl = $"{_baseUrl}/playlists/landing/{response.Content.ShareToken}/detail";
                    return response.Content;
                }

                _logger.LogWarning("Failed to generate share link for playlist {PlaylistId}", playlistId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating share link for playlist {PlaylistId}", playlistId);
                return null;
            }
        }

        public Task<string> GetShareUrlAsync(string shareToken)
        {
            return Task.FromResult($"{_baseUrl}/songs/{shareToken}");
        }

        public async Task NotifyLinkCopiedAsync()
        {
            // Platform-specific notification can be implemented here
            await Task.CompletedTask;
        }
    }
}
