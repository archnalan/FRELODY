using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPIs.Controllers;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class ShareLinkService : IShareLinkService
    {
        private readonly SongDbContext _context;
        private readonly ILogger<ShareController> _logger;

        public ShareLinkService(SongDbContext context, ILogger<ShareController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ServiceResult<ShareLinkDto>> GenerateShareLink([FromBody] ShareLinkCreateDto request, string? baseUrl)
        {
            try
            {
                // Validate that the song exists
                var songExists = await _context.Songs.AnyAsync(s => s.Id == request.SongId);
                if (!songExists)
                {
                    return ServiceResult<ShareLinkDto>.Failure(new KeyNotFoundException("Song not found"));
                }

                // 1. Try to find an existing active, non-expired share link for this song
                var now = DateTime.UtcNow;
                var existing = await _context
                    .ShareLinks
                    .Where(sl =>
                        sl.SongId != null && sl.SongId == request.SongId &&
                        (!sl.ExpiresAt.HasValue || sl.ExpiresAt > now))
                    .OrderByDescending(sl => sl.CreatedAt)
                    .FirstOrDefaultAsync();

                if (existing is not null)
                {
                    var existingDto = existing.Adapt<ShareLinkDto>();
                    existingDto.ShareUrl = $"{baseUrl}/shared/{existing.ShareToken}";
                    _logger.LogInformation("Returned existing share link (Id: {Id}) for song {SongId}", existing.Id, request.SongId);
                    return ServiceResult<ShareLinkDto>.Success(existingDto);
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

                return ServiceResult<ShareLinkDto>.Success(shareLinkDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating (or retrieving) share link for song {SongId}", request.SongId);
                return ServiceResult<ShareLinkDto>.Failure(new Exception("An error occurred while generating the share link"));
            }


        }

        public async Task<ServiceResult<SongDto>> GetSharedSong([Required] string shareToken)
        {
            try
            {
                var shareLink = await _context.ShareLinks
                    .FirstOrDefaultAsync(sl => sl.ShareToken == shareToken);

                if (shareLink == null)
                {
                    return ServiceResult<SongDto>.Failure(new KeyNotFoundException("Share link not found"));
                }

                if (shareLink.ExpiresAt.HasValue && shareLink.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return ServiceResult<SongDto>.Failure(new UnauthorizedAccessException("Share link has expired"));
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
                    return ServiceResult<SongDto>.Failure(new KeyNotFoundException("Song not found"));
                }

                var songDto = song.Adapt<SongDto>();

                _logger.LogInformation("Shared song {SongId} accessed via token {ShareToken}",
                    song.Id, shareToken);

                return ServiceResult<SongDto>.Success(songDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shared song for token {ShareToken}", shareToken);
                return ServiceResult<SongDto>.Failure(new Exception("An error occurred while retrieving the shared song"));
            }
        }

        public async Task<ServiceResult<bool>> RevokeShareLink([FromRoute] string shareToken)
        {
            try
            {
                var shareLink = await _context.ShareLinks
                    .FirstOrDefaultAsync(sl => sl.ShareToken == shareToken);

                if (shareLink == null)
                {
                    return ServiceResult<bool>.Failure(new KeyNotFoundException("Share link not found"));
                }

                shareLink.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Share link revoked for token {ShareToken}", shareToken);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking share link for token {ShareToken}", shareToken);
                return ServiceResult<bool>.Failure(new Exception("An error occurred while revoking the share link"));
            }
        }

        private string GenerateUniqueToken()
        {
            const int tokenLengthBytes = 32;
            Span<byte> tokenBytes = stackalloc byte[tokenLengthBytes];
            RandomNumberGenerator.Fill(tokenBytes);
            return WebEncoders.Base64UrlEncode(tokenBytes);
        }

    }
}
