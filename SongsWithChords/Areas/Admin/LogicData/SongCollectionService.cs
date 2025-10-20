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
    public class SongCollectionService : ISongCollectionService
    {
        private readonly string _userId;
        private readonly SongDbContext _context;
        private readonly ITenantProvider _tenantProvider;
        private readonly ILogger<SongCollectionService> _logger;

        public SongCollectionService(SongDbContext context, ILogger<SongCollectionService> logger, ITenantProvider tenantProvider)
        {
            _context = context;
            _logger = logger;
            _tenantProvider = tenantProvider;
            _userId = _tenantProvider.GetUserId();
        }

        #region Get all song collections
        public async Task<ServiceResult<List<SongCollectionDto>>> GetAllSongCollectionsAsync()
        {
            try
            {
                var collections = await _context.SongCollections
                    .Include(c => c.SongBooks)
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Title)
                    .ToListAsync();

                var collectionsDto = collections.Adapt<List<SongCollectionDto>>();

                return ServiceResult<List<SongCollectionDto>>.Success(collectionsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error retrieving song collections: {Error}", ex);
                return ServiceResult<List<SongCollectionDto>>.Failure(ex);
            }
        }
        #endregion

        #region Get User Song Collections
        public async Task<ServiceResult<List<CollectionWithSongs>>> GetUserSongCollectionsAsync(string userId)
        {
            try
            {
                var userCollections = await _context.SongCollections
                    .Where(c => c.Curator == userId)
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Title)
                    .ToListAsync();
                var collectionsWithSongs = new List<CollectionWithSongs>();
                foreach (var collection in userCollections)
                {
                    var userCollectionSongs = await _context.SongUserCollections
                        .Where(sc => sc.SongCollectionId == collection.Id)
                        .Include(sc => sc.Song)
                        .OrderBy(sc => sc.SortOrder)
                        .ToListAsync();
                    CollectionWithSongs playlist = new()
                    {
                        Playlist = collection.Adapt<SongCollectionDto>(),
                        Songs = userCollectionSongs.Select(uc => new PlaylistSongDto
                        {
                            Id = uc.Song.Id,
                            Title = uc.Song.Title,
                            SongNumber = uc.Song.SongNumber,
                            WrittenBy = uc.Song.WrittenBy,
                            SortOrder = uc.SortOrder,
                            DateScheduled = uc.DateScheduled
                        }).ToList()
                    };
                    collectionsWithSongs.Add(playlist);
                }
                return ServiceResult<List<CollectionWithSongs>>.Success(collectionsWithSongs);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error retrieving song collections for user {UserId}: {Error}", userId, ex);
                return ServiceResult<List<CollectionWithSongs>>.Failure(ex);
            }
        }
        #endregion

        #region Get song collection by Id
        public async Task<ServiceResult<CollectionWithSongs>> GetSongCollectionByIdAsync(string id)
        {
            try
            {
                var collection = await _context.SongCollections
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (collection == null)
                {
                    _logger.LogWarning("Song collection with Id {Id} not found.", id);
                    return ServiceResult<CollectionWithSongs>.Failure(
                        new KeyNotFoundException($"Song collection with Id {id} not found."));
                }

                var userCollection = await _context.SongUserCollections
                    .Where(sc => sc.SongCollectionId == id)
                    .Include(sc => sc.Song)
                    .OrderBy(sc => sc.SortOrder)
                    .ToListAsync();
                CollectionWithSongs playlist = new()
                {
                    Playlist = collection.Adapt<SongCollectionDto>(),
                    Songs = userCollection.Select(uc => new PlaylistSongDto
                    {
                        Id = uc.Song.Id,
                        Title = uc.Song.Title,
                        SongNumber = uc.Song.SongNumber,
                        WrittenBy = uc.Song.WrittenBy,
                        SortOrder = uc.SortOrder,
                        DateScheduled = uc.DateScheduled
                    }).ToList()
                };
                return ServiceResult<CollectionWithSongs>.Success(playlist);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error retrieving song collection with Id {Id}: {Error}", id, ex);
                return ServiceResult<CollectionWithSongs>.Failure(ex);
            }
        }
        #endregion

        #region Create song collection
        public async Task<ServiceResult<SongCollectionDto>> CreateSongCollectionAsync(SongCollectionDto collectionDto)
        {
            try
            {
                if (collectionDto == null)
                {
                    _logger.LogWarning("Song collection data is null.");
                    return ServiceResult<SongCollectionDto>.Failure(
                        new ArgumentNullException(nameof(collectionDto)));
                }
                var collection = collectionDto.Adapt<SongCollection>();
                await _context.SongCollections.AddAsync(collection);
                await _context.SaveChangesAsync();
                var collectionDtoResult = collection.Adapt<SongCollectionDto>();
                return ServiceResult<SongCollectionDto>.Success(collectionDtoResult);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error creating song collection: {Error}", ex);
                return ServiceResult<SongCollectionDto>.Failure(ex);
            }
        }
        #endregion

        // Add this region after the existing #region Create song collection

        #region Add collection with songs
        public async Task<ServiceResult<SongCollectionDto>> AddCollectionAsync([Required] SongCollectionCreateDto collectionCreateDto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var collection = new SongCollection
                        {
                            Title = collectionCreateDto.Title,
                            Theme = collectionCreateDto.Theme,
                            Curator = collectionCreateDto.Curator ?? _userId,
                            CollectionDate = collectionCreateDto.SheduledDate?.DateTime,
                            Slug = GenerateSlug(collectionCreateDto.Title),
                            IsPublic = true,
                            IsFeatured = false
                        };

                        await _context.SongCollections.AddAsync(collection);
                        await _context.SaveChangesAsync();

                        // Add songs to the collection if provided
                        if (collectionCreateDto.SongIds != null && collectionCreateDto.SongIds.Any())
                        {
                            var songCollectionSongs = new List<SongUserCollection>();
                            int sortOrder = 1;

                            foreach (var songId in collectionCreateDto.SongIds)
                            {
                                // Verify song exists
                                var songExists = await _context.Songs.AnyAsync(s => s.Id == songId);
                                if (!songExists)
                                {
                                    _logger.LogWarning("Song with Id {SongId} not found, skipping.", songId);
                                    continue;
                                }

                                songCollectionSongs.Add(new SongUserCollection
                                {
                                    SongId = songId,
                                    SongCollectionId = collection.Id,
                                    AddedByUserId = _userId,
                                    SortOrder = sortOrder++,
                                    DateScheduled = collectionCreateDto.SheduledDate ?? DateTimeOffset.UtcNow
                                });
                            }

                            if (songCollectionSongs.Any())
                            {
                                await _context.SongUserCollections.AddRangeAsync(songCollectionSongs);
                                await _context.SaveChangesAsync();
                            }
                        }

                        // Reload with related data
                        var createdCollection = await _context.SongCollections
                            .Include(c => c.SongCollections)
                                .ThenInclude(cs => cs.Song)
                            .Include(c => c.SongBooks)
                            .ThenInclude(sb => sb.Categories)
                                .ThenInclude(cat => cat.Songs)
                            .FirstOrDefaultAsync(c => c.Id == collection.Id);

                        var collectionDto = createdCollection.Adapt<SongCollectionDto>();
                        await transaction.CommitAsync();
                        return ServiceResult<SongCollectionDto>.Success(collectionDto);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError("Error adding song collection: {Error}", ex);
                        return ServiceResult<SongCollectionDto>.Failure(ex);
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

        public async Task<ServiceResult<SongCollectionDto>> AddSongToCollectionAsync(string collectionId, string songId)
        {
            try
            {
                bool songExists = await _context.Songs.AnyAsync(s => s.Id == songId);
                if (!songExists)
                {
                    _logger.LogWarning("Song with Id {SongId} not found.", songId);
                    return ServiceResult<SongCollectionDto>.Failure(
                        new KeyNotFoundException($"Song with Id {songId} not found."));
                }
                var collectionExists = await _context.SongCollections.AnyAsync(c => c.Id == collectionId);
                if (!collectionExists)
                {
                    _logger.LogWarning("Song collection with Id {CollectionId} not found.", collectionId);
                    return ServiceResult<SongCollectionDto>.Failure(
                        new KeyNotFoundException($"Song collection with Id {collectionId} not found."));
                }
                var songCollection = new SongUserCollection
                {
                    SongCollectionId = collectionId,
                    SongId = songId,
                    AddedByUserId = _userId,
                    DateScheduled = DateTimeOffset.UtcNow
                };
                await _context.SongUserCollections.AddAsync(songCollection);
                await _context.SaveChangesAsync();
                var collection = await _context.SongCollections
                    .Include(c => c.SongCollections)
                        .ThenInclude(sc => sc.Song)
                    .FirstOrDefaultAsync(c => c.Id == collectionId);
              
                var collectionDto = collection.Adapt<SongCollectionDto>();
                return ServiceResult<SongCollectionDto>.Success(collectionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding song with Id {SongId} to collection {CollectionId}: {Error}", songId, collectionId, ex);
                return ServiceResult<SongCollectionDto>.Failure(ex);
            }
        }
        #endregion

        #region Make collection private
        public async Task<ServiceResult<SongCollectionDto>> MakeCollectionPrivateAsync(string id)
        {
            try
            {
                var collection = await _context.SongCollections
                    .FirstOrDefaultAsync(c => c.Id == id);
                if (collection == null)
                {
                    _logger.LogWarning("Song collection with Id {Id} not found.", id);
                    return ServiceResult<SongCollectionDto>.Failure(
                        new KeyNotFoundException($"Song collection with Id {id} not found."));
                }
                collection.IsPublic = false;
                await _context.SaveChangesAsync();
                var collectionDto = collection.Adapt<SongCollectionDto>();
                return ServiceResult<SongCollectionDto>.Success(collectionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error making song collection with Id {Id} private: {Error}", id, ex);
                return ServiceResult<SongCollectionDto>.Failure(ex);
            }
        }
        #endregion

        #region Remove Song from collection
        public async Task<ServiceResult<bool>> RemoveSongFromCollectionAsync(string collectionId, string songId)
        {
            try
            {
                var songCollection = await _context.SongUserCollections
                    .FirstOrDefaultAsync(sc => sc.SongCollectionId == collectionId && sc.SongId == songId);
                if (songCollection == null)
                {
                    _logger.LogWarning("Song with Id {SongId} not found in collection {CollectionId}.", songId, collectionId);
                    return ServiceResult<bool>.Failure(
                        new KeyNotFoundException($"Song with Id {songId} not found in collection {collectionId}."));
                }
                _context.SongUserCollections.Remove(songCollection);
                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error removing song with Id {SongId} from collection {CollectionId}: {Error}", songId, collectionId, ex);
                return ServiceResult<bool>.Failure(ex);
            }
        }
        #endregion

        #region Update song collection
        public async Task<ServiceResult<SongCollectionDto>> UpdateSongCollectionAsync(string id, SongCollectionDto updatedCollection)
        {
            try
            {
                var collection = await _context.SongCollections
                    .Include(c => c.SongBooks)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (collection == null)
                {
                    _logger.LogWarning("Song collection with Id {Id} not found.", id);
                    return ServiceResult<SongCollectionDto>.Failure(
                        new KeyNotFoundException($"Song collection with Id {id} not found."));
                }
           
                var slugExists = await _context.SongCollections
                    .AnyAsync(c => c.Slug == updatedCollection.Slug && c.Id != id);
                if (slugExists)
                {
                    _logger.LogWarning("Slug {Slug} already exists for another collection.", updatedCollection.Slug);
                    return ServiceResult<SongCollectionDto>.Failure(
                        new InvalidOperationException($"Slug '{updatedCollection.Slug}' already exists."));
                }
                // Map updated properties
                collection.Title = updatedCollection.Title;
                collection.Description = updatedCollection.Description ?? collection.Description;
                collection.Slug = updatedCollection.Slug;
                collection.Curator = updatedCollection.Curator;
                collection.CollectionDate = updatedCollection.CollectionDate;
                collection.IsPublic = updatedCollection.IsPublic;
                collection.IsFeatured = updatedCollection.IsFeatured;
                collection.SortOrder = updatedCollection.SortOrder;
                collection.Theme = updatedCollection.Theme;

                await _context.SaveChangesAsync();
                var collectionDto = collection.Adapt<SongCollectionDto>();

                return ServiceResult<SongCollectionDto>.Success(collectionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating song collection with Id {Id}: {Error}", id, ex);
                return ServiceResult<SongCollectionDto>.Failure(ex);
            }
        }
        #endregion

        #region Delete song collection
        public async Task<ServiceResult<bool>> DeleteSongCollectionAsync(string id)
        {
            try
            {
                var collection = await _context.SongCollections
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (collection == null)
                {
                    _logger.LogWarning("Song collection with Id {Id} not found.", id);
                    return ServiceResult<bool>.Failure(
                        new KeyNotFoundException($"Song collection with Id {id} not found."));
                }

                var songsInCollection = await _context.SongUserCollections
                    .Where(sc => sc.SongCollectionId == id).ToListAsync();
               
                _context.SongUserCollections.RemoveRange(songsInCollection);

                _context.SongCollections.Remove(collection);
                await _context.SaveChangesAsync();

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error deleting song collection with Id {Id}: {Error}", id, ex);
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
            List<string>? curatorIds = null,
            string? orderByColumn = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                limit = limit <= 0 ? 10 : limit; // zero limit not allowed
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
                        sb.Title AS SongBookTitle,
                        sb.Slug AS SongBookSlug,
                        sb.Id AS SongBookId,
                        sb.Description AS SongBookDescription,
                        c.Id AS CategoryId,
                        c.CategorySlug,
                        CASE WHEN suf.Id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsFavorite  -- Cast to BIT for proper bool mapping
                    FROM Songs s
                    LEFT JOIN Categories c ON s.CategoryId = c.Id
                    LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                    LEFT JOIN SongCollections sc ON sb.CollectionId = sc.Id
                    LEFT JOIN SongUserFavorites suf ON suf.SongId = s.Id 
                        AND suf.UserId = @UserId
                         AND (suf.IsDeleted = 0 OR suf.IsDeleted IS NULL)
                    WHERE
                        (@SongName IS NULL OR s.Title LIKE '%' + @SongName + '%')
                        AND (@SongNumber IS NULL OR s.SongNumber = @SongNumber)
                        AND (@CategoryName IS NULL OR c.Name LIKE '%' + @CategoryName + '%')
                        AND (@SongBookId IS NULL OR sb.Id = @SongBookId)
                        AND (@CuratorIds IS NULL OR sc.Curator IN (SELECT [value] FROM OPENJSON(@CuratorIds)))
                    ORDER BY s.Title 
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
                    new SqlParameter("@CuratorIds", curatorIds != null && curatorIds.Count > 0 
                        ? JsonSerializer.Serialize(curatorIds) 
                        : (object)DBNull.Value),
                    new SqlParameter("@UserId", _userId ?? (object)DBNull.Value),
                };

                var rawResults = await _context.Database
                    .SqlQueryRaw<SongResult>(sql, parameters.ToArray())
                    .AsNoTracking()
                    .ToPaginatedResultAsync(offset,limit,cancellationToken,orderByColumn);
                
                return ServiceResult<PaginationDetails<SongResult>>.Success(rawResults);
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
                    sc.Title AS CollectionTitle,
                    sc.Curator AS CollectionCurator,
                    CASE WHEN suf.Id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsFavorite,
                    100 AS RelevanceScore,
                    'title' AS MatchType,
                    s.Title AS MatchSnippet
                FROM Songs s
                LEFT JOIN Categories c ON s.CategoryId = c.Id
                LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                LEFT JOIN SongCollections sc ON sb.CollectionId = sc.Id
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
                    sc.Title AS CollectionTitle,
                    sc.Curator AS CollectionCurator,
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
                LEFT JOIN SongCollections sc ON sb.CollectionId = sc.Id
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
                    sc.Title AS CollectionTitle,
                    sc.Curator AS CollectionCurator,
                    CASE WHEN suf.Id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsFavorite,
                    80 AS RelevanceScore,
                    'category' AS MatchType,
                    c.Name AS MatchSnippet
                FROM Songs s
                LEFT JOIN Categories c ON s.CategoryId = c.Id
                LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                LEFT JOIN SongCollections sc ON sb.CollectionId = sc.Id
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
                    sc.Title AS CollectionTitle,
                    sc.Curator AS CollectionCurator,
                    CASE WHEN suf.Id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsFavorite,
                    70 AS RelevanceScore,
                    'book' AS MatchType,
                    sb.Title AS MatchSnippet
                FROM Songs s
                LEFT JOIN Categories c ON s.CategoryId = c.Id
                LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                LEFT JOIN SongCollections sc ON sb.CollectionId = sc.Id
                LEFT JOIN SongUserFavorites suf ON suf.SongId = s.Id 
                    AND suf.UserId = @UserId
                    AND suf.IsDeleted = 0
                WHERE sb.Title LIKE @SearchPattern

                UNION ALL

                -- Collection title matches (low priority)
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
                    sc.Title AS CollectionTitle,
                    sc.Curator AS CollectionCurator,
                    CASE WHEN suf.Id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsFavorite,
                    60 AS RelevanceScore,
                    'collection' AS MatchType,
                    sc.Title AS MatchSnippet
                FROM Songs s
                LEFT JOIN Categories c ON s.CategoryId = c.Id
                LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                LEFT JOIN SongCollections sc ON sb.CollectionId = sc.Id
                LEFT JOIN SongUserFavorites suf ON suf.SongId = s.Id 
                    AND suf.UserId = @UserId
                    AND suf.IsDeleted = 0
                WHERE sc.Title LIKE @SearchPattern

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
                    sc.Title AS CollectionTitle,
                    sc.Curator AS CollectionCurator,
                    CASE WHEN suf.Id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsFavorite,
                    50 AS RelevanceScore,
                    'author' AS MatchType,
                    COALESCE(s.WrittenBy, sb.Author) AS MatchSnippet
                FROM Songs s
                LEFT JOIN Categories c ON s.CategoryId = c.Id
                LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                LEFT JOIN SongCollections sc ON sb.CollectionId = sc.Id
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
                CollectionTitle, CollectionCurator
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
                LEFT JOIN SongCollections sc ON sb.CollectionId = sc.Id
                WHERE sc.Title LIKE @SearchPattern
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