using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPIs.Areas.Admin.ViewModels;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYUI.Shared.Models.PlaylistModels;
using Mapster;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class PlaylistService : IPlaylistService
    {
        private readonly string _userId;
        private readonly SongDbContext _context;
        private readonly ITenantProvider _tenantProvider;
        private readonly ILogger<PlaylistService> _logger;

        public PlaylistService(SongDbContext context, ILogger<PlaylistService> logger, ITenantProvider tenantProvider)
        {
            _context = context;
            _logger = logger;
            _tenantProvider = tenantProvider;
            _userId = _tenantProvider.GetUserId();
        }

        #region Get all Playlists
        public async Task<ServiceResult<List<PlaylistDto>>> GetAllPlaylistsAsync()
        {
            try
            {
                var playlists = await _context.Playlists
                    .Include(c => c.SongBooks)
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Title)
                    .ToListAsync();

                var playlistsDto = playlists.Adapt<List<PlaylistDto>>();

                return ServiceResult<List<PlaylistDto>>.Success(playlistsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error retrieving song playlists: {Error}", ex);
                return ServiceResult<List<PlaylistDto>>.Failure(ex);
            }
        }
        #endregion

        #region Get User Playlists
        public async Task<ServiceResult<List<PlaylistSongs>>> GetUserPlaylistsAsync(string userId)
        {
            try
            {
                var userplaylists = await _context.Playlists
                    .Where(c => c.Curator == userId)
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Title)
                    .ToListAsync();
                var playlistsWithSongs = new List<PlaylistSongs>();
                foreach (var up in userplaylists)
                {
                    var userplaylistSongs = await _context.SongUserPlaylists
                        .Where(sc => sc.PlaylistId == up.Id)
                        .Include(sc => sc.Song)
                        .OrderBy(sc => sc.SortOrder)
                        .ToListAsync();
                    PlaylistSongs playlist = new()
                    {
                        Playlist = up.Adapt<PlaylistDto>(),
                        Songs = userplaylistSongs.Select(uc => new PlaylistSongDto
                        {
                            Id = uc.Song.Id,
                            Title = uc.Song.Title,
                            SongNumber = uc.Song.SongNumber,
                            WrittenBy = uc.Song.WrittenBy,
                            SortOrder = uc.SortOrder,
                            DateScheduled = uc.DateScheduled
                        }).ToList()
                    };
                    playlistsWithSongs.Add(playlist);
                }
                return ServiceResult<List<PlaylistSongs>>.Success(playlistsWithSongs);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error retrieving song playlists for user {UserId}: {Error}", userId, ex);
                return ServiceResult<List<PlaylistSongs>>.Failure(ex);
            }
        }
        #endregion

        #region Get playlist by Id
        public async Task<ServiceResult<PlaylistSongs>> GetPlaylistByIdAsync(string id)
        {
            try
            {
                var playlist = await _context.Playlists
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (playlist == null)
                {
                    _logger.LogWarning("Song playlist with Id {Id} not found.", id);
                    return ServiceResult<PlaylistSongs>.Failure(
                        new KeyNotFoundException($"Song playlist with Id {id} not found."));
                }

                var userPlaylist = await _context.SongUserPlaylists
                    .Where(sc => sc.PlaylistId == id)
                    .Include(sc => sc.Song)
                    .OrderBy(sc => sc.SortOrder)
                    .ToListAsync();
                PlaylistSongs playlistSongs = new()
                {
                    Playlist = playlist.Adapt<PlaylistDto>(),
                    Songs = userPlaylist.Select(uc => new PlaylistSongDto
                    {
                        Id = uc.Song.Id,
                        Title = uc.Song.Title,
                        SongNumber = uc.Song.SongNumber,
                        WrittenBy = uc.Song.WrittenBy,
                        SortOrder = uc.SortOrder,
                        DateScheduled = uc.DateScheduled
                    }).ToList()
                };
                return ServiceResult<PlaylistSongs>.Success(playlistSongs);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error retrieving song playlist with Id {Id}: {Error}", id, ex);
                return ServiceResult<PlaylistSongs>.Failure(ex);
            }
        }
        #endregion

        #region Create Playlist
        public async Task<ServiceResult<PlaylistDto>> CreatePlaylistAsync(PlaylistDto playlistDto)
        {
            try
            {
                if (playlistDto == null)
                {
                    _logger.LogWarning("Song playlist data is null.");
                    return ServiceResult<PlaylistDto>.Failure(
                        new ArgumentNullException(nameof(playlistDto)));
                }
                var playlist = playlistDto.Adapt<Playlist>();
                await _context.Playlists.AddAsync(playlist);
                await _context.SaveChangesAsync();
                var playlistDtoResult = playlist.Adapt<PlaylistDto>();
                return ServiceResult<PlaylistDto>.Success(playlistDtoResult);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error creating song playlist: {Error}", ex);
                return ServiceResult<PlaylistDto>.Failure(ex);
            }
        }
        #endregion

        #region Add playlist with songs
        public async Task<ServiceResult<PlaylistDto>> AddPlaylistAsync([Required] PlaylistCreateDto playlistCreateDto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var playlist = new Playlist
                        {
                            Title = playlistCreateDto.Title,
                            Theme = playlistCreateDto.Theme,
                            Curator = playlistCreateDto.Curator ?? _userId,
                            PlaylistDate = playlistCreateDto.SheduledDate?.DateTime,
                            Slug = GenerateSlug(playlistCreateDto.Title),
                            IsPublic = true,
                            IsFeatured = false
                        };

                        await _context.Playlists.AddAsync(playlist);
                        await _context.SaveChangesAsync();

                        // Add songs to the playlist if provided
                        if (playlistCreateDto.SongIds != null && playlistCreateDto.SongIds.Any())
                        {
                            var songplaylistSongs = new List<SongUserPlaylist>();
                            int sortOrder = 1;

                            foreach (var songId in playlistCreateDto.SongIds)
                            {
                                // Verify song exists
                                var songExists = await _context.Songs.AnyAsync(s => s.Id == songId);
                                if (!songExists)
                                {
                                    _logger.LogWarning("Song with Id {SongId} not found, skipping.", songId);
                                    continue;
                                }

                                songplaylistSongs.Add(new SongUserPlaylist
                                {
                                    SongId = songId,
                                    PlaylistId = playlist.Id,
                                    AddedByUserId = _userId,
                                    SortOrder = sortOrder++,
                                    DateScheduled = playlistCreateDto.SheduledDate ?? DateTimeOffset.UtcNow
                                });
                            }

                            if (songplaylistSongs.Any())
                            {
                                await _context.SongUserPlaylists.AddRangeAsync(songplaylistSongs);
                                await _context.SaveChangesAsync();
                            }
                        }

                        // Reload with related data
                        var createdplaylist = await _context.Playlists
                            .Include(c => c.SongPlaylists)
                                .ThenInclude(cs => cs.Song)
                            .Include(c => c.SongBooks)
                            .ThenInclude(sb => sb.Categories)
                                .ThenInclude(cat => cat.Songs)
                            .FirstOrDefaultAsync(c => c.Id == playlist.Id);

                        var playlistDto = createdplaylist.Adapt<PlaylistDto>();
                        await transaction.CommitAsync();
                        return ServiceResult<PlaylistDto>.Success(playlistDto);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError("Error adding song playlist: {Error}", ex);
                        return ServiceResult<PlaylistDto>.Failure(ex);
                    }
                }
            });           
        }

        private string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return Guid.NewGuid().ToString("N")[..8];

            return title.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("'", "")
                .Replace("\"", "")
                .Trim();
        }

        public async Task<ServiceResult<PlaylistDto>> AddSongToPlaylistAsync(string playlistId, string songId)
        {
            try
            {
                bool songExists = await _context.Songs.AnyAsync(s => s.Id == songId);
                if (!songExists)
                {
                    _logger.LogWarning("Song with Id {SongId} not found.", songId);
                    return ServiceResult<PlaylistDto>.Failure(
                        new KeyNotFoundException($"Song with Id {songId} not found."));
                }
                var playlistExists = await _context.Playlists.AnyAsync(c => c.Id == playlistId);
                if (!playlistExists)
                {
                    _logger.LogWarning("Song playlist with Id {playlistId} not found.", playlistId);
                    return ServiceResult<PlaylistDto>.Failure(
                        new KeyNotFoundException($"Song playlist with Id {playlistId} not found."));
                }

                var songAlreadyInPlaylist = await _context.SongUserPlaylists
                    .AnyAsync(sp => sp.PlaylistId == playlistId && sp.SongId == songId);
                if (songAlreadyInPlaylist)
                {
                    _logger.LogWarning("Song with Id {SongId} already exists in playlist {PlaylistId}.", songId, playlistId);
                    return ServiceResult<PlaylistDto>.Failure(
                        new InvalidOperationException($"Song is already in the playlist."));
                }

                var songplaylist = new SongUserPlaylist
                {
                    PlaylistId = playlistId,
                    SongId = songId,
                    AddedByUserId = _userId,
                    DateScheduled = DateTimeOffset.UtcNow
                };

                await _context.SongUserPlaylists.AddAsync(songplaylist);
                await _context.SaveChangesAsync();
                var playlist = await _context.Playlists
                    .Include(c => c.SongPlaylists)
                        .ThenInclude(sc => sc.Song)
                    .FirstOrDefaultAsync(c => c.Id == playlistId);

                var playlistDto = playlist.Adapt<PlaylistDto>();
                return ServiceResult<PlaylistDto>.Success(playlistDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding song with Id {SongId} to playlist {playlistId}: {Error}", songId, playlistId, ex);
                return ServiceResult<PlaylistDto>.Failure(ex);
            }
        }
        #endregion

        #region Make playlist private
        public async Task<ServiceResult<PlaylistDto>> MakePlaylistPrivateAsync(string id)
        {
            try
            {
                var playlist = await _context.Playlists
                    .FirstOrDefaultAsync(c => c.Id == id);
                if (playlist == null)
                {
                    _logger.LogWarning("Song playlist with Id {Id} not found.", id);
                    return ServiceResult<PlaylistDto>.Failure(
                        new KeyNotFoundException($"Song playlist with Id {id} not found."));
                }
                playlist.IsPublic = false;
                await _context.SaveChangesAsync();
                var playlistDto = playlist.Adapt<PlaylistDto>();
                return ServiceResult<PlaylistDto>.Success(playlistDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error making song playlist with Id {Id} private: {Error}", id, ex);
                return ServiceResult<PlaylistDto>.Failure(ex);
            }
        }
        #endregion

        #region Remove Song from playlist
        public async Task<ServiceResult<bool>> RemoveSongFromPlaylistAsync(string playlistId, string songId)
        {
            try
            {
                var songplaylist = await _context.SongUserPlaylists
                    .FirstOrDefaultAsync(sc => sc.PlaylistId == playlistId && sc.SongId == songId);
                if (songplaylist == null)
                {
                    _logger.LogWarning("Song with Id {SongId} not found in playlist {playlistId}.", songId, playlistId);
                    return ServiceResult<bool>.Failure(
                        new KeyNotFoundException($"Song with Id {songId} not found in playlist {playlistId}."));
                }
                _context.SongUserPlaylists.Remove(songplaylist);
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error removing song with Id {SongId} from playlist {playlistId}: {Error}", songId, playlistId, ex);
                return ServiceResult<bool>.Failure(ex);
            }
        }
        #endregion

        #region Update Playlist
        public async Task<ServiceResult<PlaylistDto>> UpdatePlaylistAsync(string id, PlaylistDto updatedPlaylist)
        {
            try
            {
                var playlist = await _context.Playlists
                    .Include(c => c.SongBooks)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (playlist == null)
                {
                    _logger.LogWarning("Song playlist with Id {Id} not found.", id);
                    return ServiceResult<PlaylistDto>.Failure(
                        new KeyNotFoundException($"Song playlist with Id {id} not found."));
                }
           
                var slugExists = await _context.Playlists
                    .AnyAsync(c => c.Slug == updatedPlaylist.Slug && c.Id != id);
                if (slugExists)
                {
                    _logger.LogWarning("Slug {Slug} already exists for another playlist.", updatedPlaylist.Slug);
                    return ServiceResult<PlaylistDto>.Failure(
                        new InvalidOperationException($"Slug '{updatedPlaylist.Slug}' already exists."));
                }
                // Map updated properties
                playlist.Title = updatedPlaylist.Title;
                playlist.Description = updatedPlaylist.Description ?? playlist.Description;
                playlist.Slug = updatedPlaylist.Slug;
                playlist.Curator = updatedPlaylist.Curator;
                playlist.PlaylistDate = updatedPlaylist.PlaylistDate;
                playlist.IsPublic = updatedPlaylist.IsPublic;
                playlist.IsFeatured = updatedPlaylist.IsFeatured;
                playlist.SortOrder = updatedPlaylist.SortOrder;
                playlist.Theme = updatedPlaylist.Theme;

                await _context.SaveChangesAsync();
                var playlistDto = playlist.Adapt<PlaylistDto>();

                return ServiceResult<PlaylistDto>.Success(playlistDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating song playlist with Id {Id}: {Error}", id, ex);
                return ServiceResult<PlaylistDto>.Failure(ex);
            }
        }
        #endregion

        #region Delete Playlist
        public async Task<ServiceResult<bool>> DeletePlaylistAsync(string id)
        {
            try
            {
                var playlist = await _context.Playlists
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (playlist == null)
                {
                    _logger.LogWarning("Song playlist with Id {Id} not found.", id);
                    return ServiceResult<bool>.Failure(
                        new KeyNotFoundException($"Song playlist with Id {id} not found."));
                }

                var songsInplaylist = await _context.SongUserPlaylists
                    .Where(sc => sc.PlaylistId == id).ToListAsync();
               
                _context.SongUserPlaylists.RemoveRange(songsInplaylist);

                _context.Playlists.Remove(playlist);
                await _context.SaveChangesAsync();

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error deleting song playlist with Id {Id}: {Error}", id, ex);
                return ServiceResult<bool>.Failure(ex);
            }
        }
        #endregion

        #region Get Song results with pagination
        public async Task<ServiceResult<PaginationDetails<SongResult>>> GetPaginatedSongs(
    int offset,
    int limit,
    string? songName = null,
    int? songNumber = null,
    string? categoryName = null,
    string? songBookId = null,
    string? artistId = null,
    string? albumId = null,
    List<string>? curatorIds = null,
    string? orderByColumn = null,
    CancellationToken cancellationToken = default)
        {
            try
            {
                limit = limit <= 0 ? 10 : limit;

                var sql = @"
            SELECT
                s.Id,
                s.Title,
                s.SongNumber,
                s.Slug,
                s.SongPlayLevel,
                s.WrittenDateRange,
                s.WrittenBy,
                s.History,
                c.Name AS CategoryName,
                c.Id AS CategoryId,
                c.CategorySlug,
                sb.Title AS SongBookTitle,
                sb.Slug AS SongBookSlug,
                sb.Id AS SongBookId,
                sb.Description AS SongBookDescription,
                a.Name AS ArtistName,
                a.Id AS ArtistId,
                alb.Title AS AlbumTitle,
                alb.Id AS AlbumId,
                
                -- Favorite status
                CASE WHEN suf.Id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsFavorite
                
            FROM Songs s
            
            LEFT JOIN Categories c ON s.CategoryId = c.Id
            LEFT JOIN SongBooks sb ON s.SongBookId = sb.Id OR c.SongBookId = sb.Id
            
            LEFT JOIN Artists a ON s.ArtistId = a.Id
            LEFT JOIN Albums alb ON s.AlbumId = alb.Id
            
            LEFT JOIN Playlists p ON sb.PlaylistId = p.Id
            
            LEFT JOIN SongUserFavorites suf ON suf.SongId = s.Id 
                AND suf.UserId = @UserId
                AND (suf.IsDeleted = 0 OR suf.IsDeleted IS NULL)
                
            WHERE
                (s.IsDeleted = 0 OR s.IsDeleted IS NULL)
                AND (@SongName IS NULL OR s.Title LIKE '%' + @SongName + '%')
                AND (@SongNumber IS NULL OR s.SongNumber = @SongNumber)
                AND (@CategoryName IS NULL OR c.Name LIKE '%' + @CategoryName + '%')
                AND (@SongBookId IS NULL OR sb.Id = @SongBookId)
                AND (@ArtistId IS NULL OR a.Id = @ArtistId)
                AND (@AlbumId IS NULL OR alb.Id = @AlbumId)
                AND (@CuratorIds IS NULL OR p.Curator IN (SELECT [value] FROM OPENJSON(@CuratorIds)))
                
            ORDER BY 
                CASE WHEN @OrderByColumn = 'Title' THEN s.Title END ASC,
                CASE WHEN @OrderByColumn = 'SongNumber' THEN s.SongNumber END ASC,
                CASE WHEN @OrderByColumn = 'Rating' THEN s.Rating END DESC,
                s.Title ASC  -- Default ordering
                
            OFFSET @Offset ROWS 
            FETCH NEXT @Limit ROWS ONLY";

                var parameters = new List<SqlParameter>
        {
            new SqlParameter("@Offset", offset),
            new SqlParameter("@Limit", limit),
            new SqlParameter("@SongName", songName ?? (object)DBNull.Value),
            new SqlParameter("@SongNumber", songNumber ?? (object)DBNull.Value),
            new SqlParameter("@CategoryName", categoryName ?? (object)DBNull.Value),
            new SqlParameter("@SongBookId", songBookId ?? (object)DBNull.Value),
            new SqlParameter("@ArtistId", artistId ?? (object)DBNull.Value),
            new SqlParameter("@AlbumId", albumId ?? (object)DBNull.Value),
            new SqlParameter("@CuratorIds", curatorIds != null && curatorIds.Count > 0
                ? JsonSerializer.Serialize(curatorIds)
                : (object)DBNull.Value),
            new SqlParameter("@UserId", _userId ?? (object)DBNull.Value),
            new SqlParameter("@OrderByColumn", orderByColumn ?? (object)DBNull.Value)
        };

                var rawResults = await _context.Database
                    .SqlQueryRaw<SongResult>(sql, parameters.ToArray())
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                // Get total count for pagination
                var countSql = @"
            SELECT COUNT(DISTINCT s.Id)
            FROM Songs s
            LEFT JOIN Categories c ON s.CategoryId = c.Id
            LEFT JOIN SongBooks sb ON s.SongBookId = sb.Id OR c.SongBookId = sb.Id
            LEFT JOIN Artists a ON s.ArtistId = a.Id
            LEFT JOIN Albums alb ON s.AlbumId = alb.Id
            LEFT JOIN Playlists p ON sb.PlaylistId = p.Id
            WHERE
                (s.IsDeleted = 0 OR s.IsDeleted IS NULL)
                AND (@SongName IS NULL OR s.Title LIKE '%' + @SongName + '%')
                AND (@SongNumber IS NULL OR s.SongNumber = @SongNumber)
                AND (@CategoryName IS NULL OR c.Name LIKE '%' + @CategoryName + '%')
                AND (@SongBookId IS NULL OR sb.Id = @SongBookId)
                AND (@ArtistId IS NULL OR a.Id = @ArtistId)
                AND (@AlbumId IS NULL OR alb.Id = @AlbumId)
                AND (@CuratorIds IS NULL OR p.Curator IN (SELECT [value] FROM OPENJSON(@CuratorIds)))";

                var countParams = parameters.Where(p => p.ParameterName != "@Offset"
                    && p.ParameterName != "@Limit"
                    && p.ParameterName != "@OrderByColumn").ToArray();

                var totalCount = await _context.Database
                    .SqlQueryRaw<int>(countSql, countParams)
                    .FirstOrDefaultAsync(cancellationToken);

                var paginatedResult = new PaginationDetails<SongResult>
                {
                    OffSet = offset,
                    Limit = limit,
                    TotalSize = totalCount,
                    HasMore = (offset + limit) < totalCount,
                    Data = rawResults
                };

                return ServiceResult<PaginationDetails<SongResult>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error retrieving paginated songs: {Error}", ex);
                return ServiceResult<PaginationDetails<SongResult>>.Failure(
                    new ServerErrorException("An error occurred while retrieving paginated songs."));
            }
        }
        #endregion

        #region Enhanced Song Search with Multi-Table Support
        public async Task<ServiceResult<PaginationDetails<SearchSongResult>>> EnhancedSongSearch(
            int offset,
            int limit,
            string searchTerm,
            string? orderByColumn = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return ServiceResult<PaginationDetails<SearchSongResult>>.Failure(
                        new BadRequestException("Search term cannot be empty."));
                }

                limit = limit <= 0 ? 10 : limit;
                offset = offset < 0 ? 0 : offset;

                var searchPattern = $"%{searchTerm}%";
                var searchWords = searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var hasMultipleWords = searchWords.Length > 1;

                // word-by-word search
                var firstWordPattern = hasMultipleWords ? $"%{searchWords[0]}%" : null;
                var secondWordPattern = hasMultipleWords && searchWords.Length > 1 ? $"%{searchWords[1]}%" : null;
                var sql = @"
            WITH SearchResults AS (
                -- Title matches (highest priority)
                SELECT 
                    s.Id,
                    s.Title,
                    s.SongNumber,
                    s.Slug,
                    s.SongPlayLevel,
                    s.WrittenDateRange,
                    s.WrittenBy,
                    s.History,
                    c.Name AS CategoryName,
                    sb.Title AS SongBookTitle,
                    sb.Slug AS SongBookSlug,
                    sb.Id AS SongBookId,
                    sb.Description AS SongBookDescription,
                    c.Id AS CategoryId,
                    c.CategorySlug,
                    p.Title AS PlaylistTitle,
                    p.Curator AS PlaylistCurator,
                    CASE WHEN suf.Id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsFavorite,
                    100 AS RelevanceScore,
                    'title' AS MatchType,
                    s.Title AS MatchSnippet
                FROM Songs s
                LEFT JOIN Categories c ON s.CategoryId = c.Id
                LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                LEFT JOIN Playlists p ON sb.PlaylistId = p.Id
                LEFT JOIN SongUserFavorites suf ON suf.SongId = s.Id 
                    AND suf.UserId = @UserId
                    AND suf.IsDeleted = 0
                WHERE s.Title LIKE @SearchPattern

                UNION ALL

                -- Enhanced Lyric matches with aggregated lines
                SELECT DISTINCT
                    s.Id,
                    s.Title,
                    s.SongNumber,
                    s.Slug,
                    s.SongPlayLevel,
                    s.WrittenDateRange,
                    s.WrittenBy,
                    s.History,
                    c.Name AS CategoryName,
                    sb.Title AS SongBookTitle,
                    sb.Slug AS SongBookSlug,
                    sb.Id AS SongBookId,
                    sb.Description AS SongBookDescription,
                    c.Id AS CategoryId,
                    c.CategorySlug,
                    p.Title AS PlaylistTitle,
                    p.Curator AS PlaylistCurator,
                    CASE WHEN suf.Id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsFavorite,
                    CASE 
                        WHEN aggregated_line.AggregatedLyrics LIKE @SearchPattern THEN 90
                        ELSE 85
                    END AS RelevanceScore,
                    'lyrics' AS MatchType,
                    CONCAT(
                        CASE 
                            WHEN CHARINDEX(@SearchTerm, aggregated_line.AggregatedLyrics) > 30 
                            THEN '...' 
                            ELSE ''
                        END,
                        SUBSTRING(
                            aggregated_line.AggregatedLyrics,
                            CASE 
                                WHEN CHARINDEX(@SearchTerm, aggregated_line.AggregatedLyrics) > 30 
                                THEN CHARINDEX(@SearchTerm, aggregated_line.AggregatedLyrics) - 30 
                                ELSE 1 
                            END,
                            80
                        )
                    ) AS MatchSnippet
                FROM Songs s
                INNER JOIN SongParts sp ON s.Id = sp.SongId
                INNER JOIN LyricLines ll ON sp.Id = ll.PartId
                INNER JOIN (
                    SELECT 
                        ls.LyricLineId,
                        STRING_AGG(ls.Lyric, ' ') WITHIN GROUP (ORDER BY ls.LyricOrder) AS AggregatedLyrics
                    FROM LyricSegments ls
                    GROUP BY ls.LyricLineId
                ) aggregated_line ON ll.Id = aggregated_line.LyricLineId
                LEFT JOIN Categories c ON s.CategoryId = c.Id
                LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                LEFT JOIN Playlists p ON sb.PlaylistId = p.Id
                LEFT JOIN SongUserFavorites suf ON suf.SongId = s.Id 
                    AND suf.UserId = @UserId
                    AND suf.IsDeleted = 0
                WHERE aggregated_line.AggregatedLyrics LIKE @SearchPattern

                UNION ALL

                -- Category name matches (medium priority)
                SELECT 
                    s.Id,
                    s.Title,
                    s.SongNumber,
                    s.Slug,
                    s.SongPlayLevel,
                    s.WrittenDateRange,
                    s.WrittenBy,
                    s.History,
                    c.Name AS CategoryName,
                    sb.Title AS SongBookTitle,
                    sb.Slug AS SongBookSlug,
                    sb.Id AS SongBookId,
                    sb.Description AS SongBookDescription,
                    c.Id AS CategoryId,
                    c.CategorySlug,
                    p.Title AS PlaylistTitle,
                    p.Curator AS PlaylistCurator,
                    CASE WHEN suf.Id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsFavorite,
                    80 AS RelevanceScore,
                    'category' AS MatchType,
                    c.Name AS MatchSnippet
                FROM Songs s
                LEFT JOIN Categories c ON s.CategoryId = c.Id
                LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                LEFT JOIN Playlists p ON sb.PlaylistId = p.Id
                LEFT JOIN SongUserFavorites suf ON suf.SongId = s.Id 
                    AND suf.UserId = @UserId
                    AND suf.IsDeleted = 0
                WHERE c.Name LIKE @SearchPattern

                UNION ALL

                -- SongBook title matches (medium priority)
                SELECT 
                    s.Id,
                    s.Title,
                    s.SongNumber,
                    s.Slug,
                    s.SongPlayLevel,
                    s.WrittenDateRange,
                    s.WrittenBy,
                    s.History,
                    c.Name AS CategoryName,
                    sb.Title AS SongBookTitle,
                    sb.Slug AS SongBookSlug,
                    sb.Id AS SongBookId,
                    sb.Description AS SongBookDescription,
                    c.Id AS CategoryId,
                    c.CategorySlug,
                    p.Title AS PlaylistTitle,
                    p.Curator AS PlaylistCurator,
                    CASE WHEN suf.Id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsFavorite,
                    70 AS RelevanceScore,
                    'book' AS MatchType,
                    sb.Title AS MatchSnippet
                FROM Songs s
                LEFT JOIN Categories c ON s.CategoryId = c.Id
                LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                LEFT JOIN Playlists p ON sb.PlaylistId = p.Id
                LEFT JOIN SongUserFavorites suf ON suf.SongId = s.Id 
                    AND suf.UserId = @UserId
                    AND suf.IsDeleted = 0
                WHERE sb.Title LIKE @SearchPattern

                UNION ALL

                -- Playlist title matches (low priority)
                SELECT 
                    s.Id,
                    s.Title,
                    s.SongNumber,
                    s.Slug,
                    s.SongPlayLevel,
                    s.WrittenDateRange,
                    s.WrittenBy,
                    s.History,
                    c.Name AS CategoryName,
                    sb.Title AS SongBookTitle,
                    sb.Slug AS SongBookSlug,
                    sb.Id AS SongBookId,
                    sb.Description AS SongBookDescription,
                    c.Id AS CategoryId,
                    c.CategorySlug,
                    p.Title AS PlaylistTitle,
                    p.Curator AS PlaylistCurator,
                    CASE WHEN suf.Id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsFavorite,
                    60 AS RelevanceScore,
                    'Playlist' AS MatchType,
                    p.Title AS MatchSnippet
                FROM Songs s
                LEFT JOIN Categories c ON s.CategoryId = c.Id
                LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                LEFT JOIN Playlists p ON sb.PlaylistId = p.Id
                LEFT JOIN SongUserFavorites suf ON suf.SongId = s.Id 
                    AND suf.UserId = @UserId
                    AND suf.IsDeleted = 0
                WHERE p.Title LIKE @SearchPattern

                UNION ALL

                -- Author/Writer matches (lowest priority)
                SELECT 
                    s.Id,
                    s.Title,
                    s.SongNumber,
                    s.Slug,
                    s.SongPlayLevel,
                    s.WrittenDateRange,
                    s.WrittenBy,
                    s.History,
                    c.Name AS CategoryName,
                    sb.Title AS SongBookTitle,
                    sb.Slug AS SongBookSlug,
                    sb.Id AS SongBookId,
                    sb.Description AS SongBookDescription,
                    c.Id AS CategoryId,
                    c.CategorySlug,
                    p.Title AS playlistTitle,
                    p.Curator AS playlistCurator,
                    CASE WHEN suf.Id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsFavorite,
                    50 AS RelevanceScore,
                    'author' AS MatchType,
                    COALESCE(s.WrittenBy, sb.Author) AS MatchSnippet
                FROM Songs s
                LEFT JOIN Categories c ON s.CategoryId = c.Id
                LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                LEFT JOIN Playlists p ON sb.PlaylistId = p.Id
                LEFT JOIN SongUserFavorites suf ON suf.SongId = s.Id 
                    AND suf.UserId = @UserId
                    AND suf.IsDeleted = 0
                WHERE (s.WrittenBy LIKE @SearchPattern OR sb.Author LIKE @SearchPattern)
              ),
            RankedResults AS (
                SELECT *,
                       ROW_NUMBER() OVER (
                           PARTITION BY Id 
                           ORDER BY RelevanceScore DESC
                       ) as MatchRank
                FROM SearchResults
            )
            SELECT 
                Id, Title, SongNumber, Slug, SongPlayLevel, WrittenDateRange, 
                WrittenBy, History, CategoryName, SongBookTitle, SongBookSlug, 
                SongBookId, SongBookDescription, CategoryId, CategorySlug, 
                IsFavorite, RelevanceScore, MatchType, MatchSnippet,
                PlaylistTitle, PlaylistCurator
            FROM RankedResults
            WHERE MatchRank = 1
            ORDER BY RelevanceScore DESC, Title ASC
            OFFSET @Offset ROWS 
            FETCH NEXT @Limit ROWS ONLY";

                var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Offset", offset),
                new SqlParameter("@Limit", limit),
                new SqlParameter("@SearchPattern", searchPattern),
                new SqlParameter("@SearchTerm", searchTerm),
                new SqlParameter("@UserId", _userId ?? (object)DBNull.Value),
                new SqlParameter("@HasMultipleWords", hasMultipleWords),
            };

                // Add word patterns if we have multiple words
                if (hasMultipleWords)
                {
                    parameters.Add(new SqlParameter("@FirstWordPattern", firstWordPattern));
                    if (!string.IsNullOrEmpty(secondWordPattern))
                    {
                        parameters.Add(new SqlParameter("@SecondWordPattern", secondWordPattern));
                    }
                }
                else
                {
                    parameters.Add(new SqlParameter("@FirstWordPattern", DBNull.Value));
                    parameters.Add(new SqlParameter("@SecondWordPattern", DBNull.Value));
                }

                // Get the results
                var results = await _context.Database
                    .SqlQueryRaw<SearchSongResult>(sql, parameters.ToArray())
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                // Get total count for pagination
                var countSql = @"
            SELECT COUNT(DISTINCT Combined.Id) AS Value
            FROM (
                SELECT s.Id FROM Songs s WHERE s.Title LIKE @SearchPattern
                UNION
                SELECT DISTINCT s.Id FROM Songs s
                INNER JOIN SongParts sp ON s.Id = sp.SongId
                INNER JOIN LyricLines ll ON sp.Id = ll.PartId
                INNER JOIN LyricSegments ls ON ll.Id = ls.LyricLineId
                WHERE ls.Lyric LIKE @SearchPattern
                UNION
                SELECT s.Id FROM Songs s
                LEFT JOIN Categories c ON s.CategoryId = c.Id
                WHERE c.Name LIKE @SearchPattern
                UNION
                SELECT s.Id FROM Songs s
                LEFT JOIN Categories c ON s.CategoryId = c.Id
                LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                WHERE sb.Title LIKE @SearchPattern
                UNION
                SELECT s.Id FROM Songs s
                LEFT JOIN Categories c ON s.CategoryId = c.Id
                LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                LEFT JOIN Playlists p ON sb.PlaylistId = p.Id
                WHERE p.Title LIKE @SearchPattern
                UNION
                SELECT s.Id FROM Songs s
                LEFT JOIN Categories c ON s.CategoryId = c.Id
                LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                WHERE s.WrittenBy LIKE @SearchPattern OR sb.Author LIKE @SearchPattern
            ) AS Combined";

                var totalCount = await _context.Database
                    .SqlQueryRaw<int>(countSql,
                        new SqlParameter("@SearchPattern", searchPattern))
                    .FirstOrDefaultAsync(cancellationToken);

                var paginatedResult = new PaginationDetails<SearchSongResult>
                {
                    OffSet = offset,
                    Limit = limit,
                    TotalSize = totalCount,
                    HasMore = (offset + limit) < totalCount,
                    Data = results
                };

                return ServiceResult<PaginationDetails<SearchSongResult>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in song search: {Error}", ex);
                return ServiceResult<PaginationDetails<SearchSongResult>>.Failure(
                    new ServerErrorException("An error occurred while searching songs."));
            }
        }
        #endregion
    }
}