using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPIs.Controllers;
using FRELODYAPIs.Services.OgCard;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;
using FRELODYUI.Shared.Models.PlaylistModels;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class ShareLinkService : IShareLinkService
    {
        private readonly SongDbContext _context;
        private readonly ILogger<ShareController> _logger;
        private readonly IOgCardService _ogCardService;
        private readonly ShareLandingOptions _shareOptions;

        public ShareLinkService(
            SongDbContext context,
            ILogger<ShareController> logger,
            IOgCardService ogCardService,
            IOptions<ShareLandingOptions> shareOptions)
        {
            _context = context;
            _logger = logger;
            _ogCardService = ogCardService;
            _shareOptions = shareOptions.Value;
        }

        public async Task<ServiceResult<ShareLinkDto>> GenerateShareLink([FromBody] ShareLinkCreateDto request, string? baseUrl)
        {
            try
            {
                bool isSongLink = !string.IsNullOrEmpty(request.SongId);
                bool isPlaylistLink = !string.IsNullOrEmpty(request.PlaylistId);

                if (!isSongLink && !isPlaylistLink)
                {
                    return ServiceResult<ShareLinkDto>.Failure(new ArgumentException("Either SongId or PlaylistId must be provided"));
                }

                // Validate that the referenced entity exists
                if (isSongLink)
                {
                    var songExists = await _context.Songs.AnyAsync(s => s.Id == request.SongId);
                    if (!songExists)
                        return ServiceResult<ShareLinkDto>.Failure(new KeyNotFoundException("Song not found"));
                }

                if (isPlaylistLink)
                {
                    var playlistExists = await _context.Playlists.AnyAsync(p => p.Id == request.PlaylistId);
                    if (!playlistExists)
                        return ServiceResult<ShareLinkDto>.Failure(new KeyNotFoundException("Playlist not found"));
                }

                // 1. Try to find an existing active, non-expired share link
                var now = DateTime.UtcNow;
                var query = _context.ShareLinks
                    .Where(sl => !sl.ExpiresAt.HasValue || sl.ExpiresAt > now);

                if (isSongLink)
                    query = query.Where(sl => sl.SongId != null && sl.SongId == request.SongId && sl.PlaylistId == null);
                else
                    query = query.Where(sl => sl.PlaylistId != null && sl.PlaylistId == request.PlaylistId);

                var existing = await query
                    .OrderByDescending(sl => sl.CreatedAt)
                    .FirstOrDefaultAsync();

                if (existing is not null)
                {
                    var existingDto = existing.Adapt<ShareLinkDto>();
                    var publicOrigin = ResolvePublicOrigin(baseUrl);
                    existingDto.ShareUrl = isSongLink
                        ? $"{publicOrigin}/shared/{existing.ShareToken}"
                        : $"{publicOrigin}/shared/playlist/{existing.ShareToken}";
                    _logger.LogInformation("Returned existing share link (Id: {Id}) for {Type} {EntityId}",
                        existing.Id, isSongLink ? "song" : "playlist", isSongLink ? request.SongId : request.PlaylistId);
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
                    SongId = isSongLink ? request.SongId : null,
                    PlaylistId = isPlaylistLink ? request.PlaylistId : null,
                    ShareToken = shareToken,
                    CreatedAt = now,
                    ExpiresAt = expiresAt,
                    IsActive = true
                };

                // Snapshot Open Graph preview data so crawlers (WhatsApp, iMessage,
                // Facebook, Twitter, LinkedIn, Slack, Discord …) can render a rich
                // preview without JS or auth. Failures are non-fatal.
                await PopulateOgSnapshotAsync(shareLink, isSongLink);

                _context.ShareLinks.Add(shareLink);
                await _context.SaveChangesAsync();

                var shareLinkDto = shareLink.Adapt<ShareLinkDto>();
                var publicOriginNew = ResolvePublicOrigin(baseUrl);
                shareLinkDto.ShareUrl = isSongLink
                    ? $"{publicOriginNew}/shared/{shareToken}"
                    : $"{publicOriginNew}/shared/playlist/{shareToken}";

                _logger.LogInformation("New share link generated for {Type} {EntityId} with token {ShareToken}",
                    isSongLink ? "song" : "playlist", isSongLink ? request.SongId : request.PlaylistId, shareToken);

                return ServiceResult<ShareLinkDto>.Success(shareLinkDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating (or retrieving) share link for request {@Request}", request);
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

        public async Task<ServiceResult<PlaylistSongs>> GetSharedPlaylist([Required] string shareToken)
        {
            try
            {
                var shareLink = await _context.ShareLinks
                    .FirstOrDefaultAsync(sl => sl.ShareToken == shareToken);

                if (shareLink == null)
                {
                    return ServiceResult<PlaylistSongs>.Failure(new KeyNotFoundException("Share link not found"));
                }

                if (shareLink.ExpiresAt.HasValue && shareLink.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return ServiceResult<PlaylistSongs>.Failure(new UnauthorizedAccessException("Share link has expired"));
                }

                if (string.IsNullOrEmpty(shareLink.PlaylistId))
                {
                    return ServiceResult<PlaylistSongs>.Failure(new ArgumentException("This share link is not for a playlist"));
                }

                var playlist = await _context.Playlists
                    .FirstOrDefaultAsync(p => p.Id == shareLink.PlaylistId);

                if (playlist == null)
                {
                    _logger.LogWarning("Playlist {PlaylistId} not found for share token {ShareToken}",
                        shareLink.PlaylistId, shareToken);
                    return ServiceResult<PlaylistSongs>.Failure(new KeyNotFoundException("Playlist not found"));
                }

                var userPlaylist = await _context.SongUserPlaylists
                    .Where(sc => sc.PlaylistId == shareLink.PlaylistId)
                    .Include(sc => sc.Song)
                    .OrderBy(sc => sc.SortOrder)
                    .ToListAsync();

                var playlistSongs = new PlaylistSongs
                {
                    Playlist = playlist.Adapt<PlaylistDto>(),
                    Songs = userPlaylist
                        .Where(uc => uc.Song != null)
                        .Select(uc => new PlaylistSongDto
                    {
                        Id = uc.Song!.Id,
                        Title = uc.Song.Title,
                        SongNumber = uc.Song.SongNumber,
                        WrittenBy = uc.Song.WrittenBy,
                        Key = uc.Song.Key,
                        Transpose = uc.Transpose,
                        SortOrder = uc.SortOrder,
                        DateScheduled = uc.DateScheduled
                    }).ToList()
                };

                // Resolve curator ID to display name
                if (!string.IsNullOrEmpty(playlist.Curator))
                {
                    var curatorUser = await _context.Users
                        .Where(u => u.Id == playlist.Curator)
                        .Select(u => new { u.FirstName, u.LastName })
                        .FirstOrDefaultAsync();
                    if (curatorUser != null)
                        playlistSongs.Playlist.Curator = $"{curatorUser.FirstName} {curatorUser.LastName}".Trim();
                }

                _logger.LogInformation("Shared playlist {PlaylistId} accessed via token {ShareToken}",
                    playlist.Id, shareToken);

                return ServiceResult<PlaylistSongs>.Success(playlistSongs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shared playlist for token {ShareToken}", shareToken);
                return ServiceResult<PlaylistSongs>.Failure(new Exception("An error occurred while retrieving the shared playlist"));
            }
        }

        private string GenerateUniqueToken()
        {
            const int tokenLengthBytes = 32;
            Span<byte> tokenBytes = stackalloc byte[tokenLengthBytes];
            RandomNumberGenerator.Fill(tokenBytes);
            return WebEncoders.Base64UrlEncode(tokenBytes);
        }

        /// <summary>
        /// Resolves the user-facing origin used in the copied share URL. Prefers
        /// <c>ShareLanding:PublicBaseUrl</c> so links point at the public host
        /// (behind nginx in docker/prod) and fall back to the controller-supplied
        /// request origin for local development where no public origin is set.
        /// </summary>
        private string ResolvePublicOrigin(string? requestOrigin)
        {
            var origin = !string.IsNullOrWhiteSpace(_shareOptions.PublicBaseUrl)
                ? _shareOptions.PublicBaseUrl
                : requestOrigin;
            return (origin ?? string.Empty).TrimEnd('/');
        }

        private async Task PopulateOgSnapshotAsync(ShareLink shareLink, bool isSongLink)
        {
            try
            {
                OgCardContent? content = isSongLink
                    ? await BuildSongCardContentAsync(shareLink.SongId!)
                    : await BuildPlaylistCardContentAsync(shareLink.PlaylistId!);

                if (content is null) return;

                shareLink.OgTitle = Truncate(content.Title, 200);
                shareLink.OgDescription = Truncate(
                    string.Join(" · ", new[] { content.Subtitle, content.Tagline, content.Meta }
                        .Where(s => !string.IsNullOrWhiteSpace(s))),
                    500);
                shareLink.OgImagePath = await _ogCardService.RenderPngAsync(content, shareLink.ShareToken);
                shareLink.OgHtml = _ogCardService.BuildHeroHtml(content);
            }
            catch (Exception ex)
            {
                // Snapshot generation must never block share-link creation.
                _logger.LogWarning(ex, "Failed to populate OG snapshot for token {Token}", shareLink.ShareToken);
            }
        }

        private async Task<OgCardContent?> BuildSongCardContentAsync(string songId)
        {
            var song = await _context.Songs
                .Where(s => s.Id == songId)
                .Select(s => new
                {
                    s.Title,
                    s.SongNumber,
                    s.WrittenBy,
                    s.ArtistId,
                    s.AlbumId
                })
                .FirstOrDefaultAsync();

            if (song is null) return null;

            string? artistName = null;
            if (!string.IsNullOrEmpty(song.ArtistId))
            {
                artistName = await _context.Artists
                    .Where(a => a.Id == song.ArtistId)
                    .Select(a => a.Name)
                    .FirstOrDefaultAsync();
            }

            string? albumTitle = null;
            if (!string.IsNullOrEmpty(song.AlbumId))
            {
                albumTitle = await _context.Albums
                    .Where(a => a.Id == song.AlbumId)
                    .Select(a => a.Title)
                    .FirstOrDefaultAsync();
            }

            var subtitle = artistName ?? song.WrittenBy;
            var tagline = albumTitle;
            var meta = song.SongNumber.HasValue ? $"#{song.SongNumber} · Song" : "Song";

            return new OgCardContent(OgCardKind.Song, song.Title ?? "Untitled", subtitle, tagline, meta);
        }

        private async Task<OgCardContent?> BuildPlaylistCardContentAsync(string playlistId)
        {
            var playlist = await _context.Playlists
                .Where(p => p.Id == playlistId)
                .Select(p => new
                {
                    p.Title,
                    p.Description,
                    p.Theme,
                    p.Curator,
                    SongCount = p.SongPlaylists != null ? p.SongPlaylists.Count : 0
                })
                .FirstOrDefaultAsync();

            if (playlist is null) return null;

            // Resolve curator display name when Curator is a user id.
            string? curatorDisplay = playlist.Curator;
            if (!string.IsNullOrWhiteSpace(playlist.Curator))
            {
                var curatorUser = await _context.Users
                    .Where(u => u.Id == playlist.Curator)
                    .Select(u => new { u.FirstName, u.LastName })
                    .FirstOrDefaultAsync();
                if (curatorUser != null)
                {
                    var name = $"{curatorUser.FirstName} {curatorUser.LastName}".Trim();
                    if (!string.IsNullOrWhiteSpace(name)) curatorDisplay = name;
                }
            }

            var subtitle = !string.IsNullOrWhiteSpace(playlist.Theme) ? playlist.Theme : playlist.Description;
            var tagline = !string.IsNullOrWhiteSpace(curatorDisplay) ? $"Curated by {curatorDisplay}" : null;
            var meta = playlist.SongCount == 1 ? "1 song" : $"{playlist.SongCount} songs";

            return new OgCardContent(OgCardKind.Playlist, playlist.Title ?? "Untitled", subtitle, tagline, meta);
        }

        private static string? Truncate(string? s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Length <= max ? s : s[..(max - 1)] + "…";
        }

    }
}
