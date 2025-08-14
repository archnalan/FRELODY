using AutoMapper;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos;
using FRELODYLIB.Models;
using FRELODYSHRD.Dtos;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace FRELODYAPIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShareController : ControllerBase
    {
        private readonly SongDbContext _context;
        private readonly ILogger<ShareController> _logger;

        public ShareController(SongDbContext context, ILogger<ShareController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("generate-share-link")]
        public async Task<IActionResult> GenerateShareLink([FromBody] ShareLinkCreateDto request)
        {
            try
            {
                // Validate that the song exists
                var songExists = await _context.Songs.AnyAsync(s => s.Id == request.SongId);
                if (!songExists)
                {
                    return NotFound("Song not found");
                }

                // 1. Try to find an existing active, non-expired share link for this song
                var now = DateTime.UtcNow;
                var existing = await _context
                    .ShareLinks
                    .Where(sl =>
                        sl.SongId == request.SongId &&
                        sl.IsActive &&
                        (!sl.ExpiresAt.HasValue || sl.ExpiresAt > now))
                    .OrderByDescending(sl => sl.CreatedAt)
                    .FirstOrDefaultAsync();

                var baseUrl = GetBaseUrl();

                if (existing is not null)
                {
                    var existingDto = existing.Adapt<ShareLinkDto>();
                    existingDto.ShareUrl = $"{baseUrl}/shared/{existing.ShareToken}";
                    _logger.LogInformation("Returned existing share link (Id: {Id}) for song {SongId}", existing.Id, request.SongId);
                    return Ok(existingDto);
                }

                // 2. No reusable link found; generate a new one
                var shareToken = GenerateUniqueToken();

                var expiresAt = request.ExpirationDays.HasValue
                    ? now.AddDays(request.ExpirationDays.Value)
                    : now.AddDays(30); // Default 30 days

                var shareLink = new ShareLink
                {
                    Id = Guid.NewGuid().ToString(),
                    SongId = request.SongId,
                    ShareToken = shareToken,
                    CreatedAt = now,
                    ExpiresAt = expiresAt,
                    IsActive = true
                };

                _context.ShareLinks.Add(shareLink);
                await _context.SaveChangesAsync();

                var shareLinkDto = shareLink.Adapt<ShareLinkDto>();
                shareLinkDto.ShareUrl = $"{baseUrl}/shared/{shareToken}";

                _logger.LogInformation("New share link generated for song {SongId} with token {ShareToken}", request.SongId, shareToken);

                return Ok(shareLinkDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating (or retrieving) share link for song {SongId}", request.SongId);
                return StatusCode(500, "An error occurred while generating the share link");
            }
        }

        [HttpGet("get-shared-song/{shareToken}")]
        public async Task<IActionResult> GetSharedSong([FromRoute] string shareToken)
        {
            try
            {
                var shareLink = await _context.ShareLinks
                    .FirstOrDefaultAsync(sl => sl.ShareToken == shareToken && sl.IsActive);

                if (shareLink == null)
                {
                    return NotFound("Share link not found");
                }

                if (shareLink.ExpiresAt.HasValue && shareLink.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return StatusCode(403, "Share link has expired");
                }

                var song = await _context.Songs
                    .Include(s => s.SongParts)
                        .ThenInclude(sp => sp.LyricLines)
                            .ThenInclude(ll => ll.LyricSegments)
                                .ThenInclude(ls => ls.Chord)
                    .FirstOrDefaultAsync(s => s.Id == shareLink.SongId);

                if (song == null)
                {
                    _logger.LogWarning("Song {SongId} not found for share token {ShareToken}",
                        shareLink.SongId, shareToken);
                    return NotFound("Song not found");
                }

                var songDto = song.Adapt<SongDto>();

                _logger.LogInformation("Shared song {SongId} accessed via token {ShareToken}",
                    song.Id, shareToken);

                return Ok(songDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shared song for token {ShareToken}", shareToken);
                return StatusCode(500, "An error occurred while retrieving the shared song");
            }
        }

        [HttpDelete("revoke-share-link/{shareToken}")]
        public async Task<IActionResult> RevokeShareLink([FromRoute] string shareToken)
        {
            try
            {
                var shareLink = await _context.ShareLinks
                    .FirstOrDefaultAsync(sl => sl.ShareToken == shareToken);

                if (shareLink == null)
                {
                    return NotFound("Share link not found");
                }

                shareLink.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Share link revoked for token {ShareToken}", shareToken);

                return Ok(new { message = "Share link revoked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking share link for token {ShareToken}", shareToken);
                return StatusCode(500, "An error occurred while revoking the share link");
            }
        }

        private string GenerateUniqueToken()
        {
            const int tokenLengthBytes = 32;
            Span<byte> tokenBytes = stackalloc byte[tokenLengthBytes];
            RandomNumberGenerator.Fill(tokenBytes);
            return WebEncoders.Base64UrlEncode(tokenBytes);
        }

        private string GetBaseUrl()
        {
            var request = HttpContext.Request;
            return $"{request.Scheme}://{request.Host}";
        }
    }
}
