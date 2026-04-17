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

        public ShareService(
            IShareApi shareApi,
            IClipboardService clipboardService,
            ILogger<ShareService> logger)
        {
            _shareApi = shareApi;
            _clipboardService = clipboardService;
            _logger = logger;
        }

        public async Task<ShareLinkDto?> GenerateShareLinkAsync(string songId)
        {
            try
            {
                var request = new ShareLinkCreateDto { SongId = songId };
                var response = await _shareApi.GenerateShareLink(request);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    // The API builds the share URL from its own Request.Scheme/Host so the
                    // link points at the host that actually serves the OG-enabled
                    // /shared/{token} route. Never overwrite it here — in dev the UI runs
                    // on a different port than the API, and in prod nginx routes /shared/*
                    // to the API. Crawlers (WhatsApp, iMessage, Facebook, Twitter, LinkedIn,
                    // Slack, Discord, …) hit that URL and receive pre-rendered OG meta.
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

        public async Task<string> GetShareUrlAsync(string shareToken)
        {
            // No server round-trip needed — the server-provided ShareUrl is authoritative.
            // Callers generally already have a ShareLinkDto from the generate methods above.
            await Task.CompletedTask;
            return string.Empty;
        }

        public async Task NotifyLinkCopiedAsync()
        {
            // Platform-specific notification can be implemented here
            await Task.CompletedTask;
        }
    }
}
