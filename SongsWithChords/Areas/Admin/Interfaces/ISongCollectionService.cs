using FRELODYAPIs.Areas.Admin.ViewModels;
using FRELODYAPP.Dtos;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYUI.Shared.Models.PlaylistModels;
using System.ComponentModel.DataAnnotations;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ISongCollectionService
    {
        Task<ServiceResult<SongCollectionDto>> CreateSongCollectionAsync(SongCollectionDto collection);
        Task<ServiceResult<bool>> DeleteSongCollectionAsync(string id);
        Task<ServiceResult<List<SongCollectionDto>>> GetAllSongCollectionsAsync();
        Task<ServiceResult<PaginationDetails<SongResult>>> GetPaginatedSongs(int offset, int limit, string? songName = null, int? songNumber = null, string? categoryName = null, string? songBookId = null, List<string>? curatorIds = null, string? orderByColumn = null, CancellationToken cancellationToken = default);
        Task<ServiceResult<CollectionWithSongs>> GetSongCollectionByIdAsync(string id);
        Task<ServiceResult<PaginationDetails<SearchSongResult>>> EnhancedSongSearch(int offset, int limit, string searchTerm, string? orderByColumn = null, CancellationToken cancellationToken = default);
        Task<ServiceResult<SongCollectionDto>> UpdateSongCollectionAsync(string id, SongCollectionDto updatedCollection);
        Task<ServiceResult<List<CollectionWithSongs>>> GetUserSongCollectionsAsync(string userId);
        Task<ServiceResult<SongCollectionDto>> MakeCollectionPrivateAsync(string id);
        Task<ServiceResult<SongCollectionDto>> AddCollectionAsync([Required] SongCollectionCreateDto collectionCreateDto);
        Task<ServiceResult<bool>> RemoveSongFromCollectionAsync(string collectionId, string songId);
        Task<ServiceResult<SongCollectionDto>> AddSongToCollectionAsync(string collectionId, string songId);
    }
}