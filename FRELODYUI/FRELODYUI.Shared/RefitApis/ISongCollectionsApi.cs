using FRELODYAPIs.Areas.Admin.ViewModels;
using FRELODYAPP.Dtos;
using FRELODYLIB.ServiceHandler;
using FRELODYSHRD.Dtos.CreateDtos;
using Refit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface ISongCollectionsApi
    {
        [Get("/api/song-collections/get-all-song-collections")]
        Task<IApiResponse<List<SongCollectionDto>>> GetAllSongCollections();

        [Get("/api/song-collections/get-user-song-collections")]
        Task<IApiResponse<List<SongCollectionDto>>> GetUserSongCollections([Query] string userId);

        [Get("/api/song-collections/get-song-collection-by-id")]
        Task<IApiResponse<SongCollectionDto>> GetSongCollectionById([Query] string id);

        [Post("/api/song-collections/create-song-collection")]
        Task<IApiResponse<SongCollectionDto>> CreateSongCollection([Body] SongCollectionDto collection);
        
        [Post("/api/song-collections/add-collection")]
        Task<IApiResponse<SongCollectionDto>> AddCollection([Body] SongCollectionCreateDto collection);

        
        [Post("/api/song-collections/make-collection-private")]
        Task<IApiResponse<SongCollectionDto>> MakeCollectionPrivate([Query] string id);

        [Put("/api/song-collections/update-song-collection")]
        Task<IApiResponse<SongCollectionDto>> UpdateSongCollection([Query] string id, [Body] SongCollectionDto updatedCollection);

        [Delete("/api/song-collections/delete-song-collection")]
        Task<IApiResponse<bool>> DeleteSongCollection([Query] string id);

        [Get("/api/song-collections/get-paginated-songs")]
        Task<IApiResponse<PaginationDetails<SongResult>>> GetPaginatedSongs(
        [Query] int offset,
        [Query] int limit,
        [Query] string? songName = null,
        [Query] int? songNumber = null,
        [Query] string? categoryName = null,
        [Query] string? songBookId = null,
        [Query] List<string>? curatorIds = null,
        [Query] string? orderByColumn = null,
        CancellationToken cancellationToken = default);
      
        [Get("/api/song-collections/enhanced-song-search")]
        Task<IApiResponse<PaginationDetails<SearchSongResult>>> EnhancedSongSearch(
            [Query] int offset,
            [Query] int limit,
            [Query] string searchTerm,
            [Query] string? orderByColumn = null,
            CancellationToken cancellationToken = default);            
    }
}