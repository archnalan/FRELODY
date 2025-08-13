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
using System.Text;

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

                // Generate a unique share token
                var shareToken = GenerateUniqueToken();

                // Calculate expiration date
                var expiresAt = request.ExpirationDays.HasValue
                    ? DateTime.UtcNow.AddDays(request.ExpirationDays.Value)
                    : DateTime.UtcNow.AddDays(30); // Default 30 days

                // Create share link entity
                var shareLink = new ShareLink
                {
                    Id = Guid.NewGuid().ToString(),
                    SongId = request.SongId,
                    ShareToken = shareToken,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    IsActive = true
                };

                _context.ShareLinks.Add(shareLink);
                await _context.SaveChangesAsync();

                // Map to DTO
                var shareLinkDto = shareLink.Adapt<ShareLinkDto>();

                // Set the share URL (this should be configurable based on environment)
                var baseUrl = GetBaseUrl();
                shareLinkDto.ShareUrl = $"{baseUrl}/shared/{shareToken}";

                _logger.LogInformation("Share link generated for song {SongId} with token {ShareToken}",
                    request.SongId, shareToken);

                return Ok(shareLinkDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating share link for song {SongId}", request.SongId);
                return StatusCode(500, "An error occurred while generating the share link");
            }
        }

        [HttpGet("get-shared-song/{shareToken}")]
        public async Task<IActionResult> GetSharedSong([FromRoute] string shareToken)
        {
            try
            {
                // Find the share link
                var shareLink = await _context.ShareLinks
                    .FirstOrDefaultAsync(sl => sl.ShareToken == shareToken && sl.IsActive);

                if (shareLink == null)
                {
                    return NotFound("Share link not found");
                }

                // Check if expired
                if (shareLink.ExpiresAt.HasValue && shareLink.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return StatusCode(403, "Share link has expired");
                }

                // Get the song with all its data
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
            const int tokenLengthBytes = 32; // 256-bit
            Span<byte> tokenBytes = stackalloc byte[tokenLengthBytes];
            RandomNumberGenerator.Fill(tokenBytes);

            // Base64Url (RFC 4648 §5) – URL safe, no padding, no manual Replace required
            return WebEncoders.Base64UrlEncode(tokenBytes);
        }

        private string GetBaseUrl()
        {
            var request = HttpContext.Request;
            return $"{request.Scheme}://{request.Host}";
        }
    }
}
