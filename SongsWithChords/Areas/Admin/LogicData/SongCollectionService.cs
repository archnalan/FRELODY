using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPIs.Areas.Admin.ViewModels;
using FRELODYAPP.Data.Infrastructure;
using FRELODYAPP.Dtos;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler;
using FRELODYLIB.ServiceHandler.ResultModels;
using Mapster;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class SongCollectionService : ISongCollectionService
    {
        private readonly SongDbContext _context;
        private readonly ILogger<SongCollectionService> _logger;

        public SongCollectionService(SongDbContext context, ILogger<SongCollectionService> logger)
        {
            _context = context;
            _logger = logger;
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

        #region Get song collection by Id
        public async Task<ServiceResult<SongCollectionDto>> GetSongCollectionByIdAsync(string id)
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

                var collectionDto = collection.Adapt<SongCollectionDto>();
                return ServiceResult<SongCollectionDto>.Success(collectionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error retrieving song collection with Id {Id}: {Error}", id, ex);
                return ServiceResult<SongCollectionDto>.Failure(ex);
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

                collection.IsDeleted = true;
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
                        s.IsFavorite
                    FROM Songs s
                    LEFT JOIN Categories c ON s.CategoryId = c.Id
                    LEFT JOIN SongBooks sb ON c.SongBookId = sb.Id
                    LEFT JOIN SongCollections sc ON sb.CollectionId = sc.Id
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
    }
}