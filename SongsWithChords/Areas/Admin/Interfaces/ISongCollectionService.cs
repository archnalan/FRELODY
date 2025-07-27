using FRELODYAPIs.Areas.Admin.ViewModels;
using FRELODYAPP.Dtos;
using FRELODYAPP.ServiceHandler;
using FRELODYLIB.Models;
using FRELODYLIB.ServiceHandler;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface ISongCollectionService
    {
        Task<ServiceResult<SongCollectionDto>> CreateSongCollectionAsync(SongCollectionDto collection);
        Task<ServiceResult<bool>> DeleteSongCollectionAsync(string id);
        Task<ServiceResult<List<SongCollectionDto>>> GetAllSongCollectionsAsync();
        Task<ServiceResult<PaginationDetails<SongResult>>> GetPaginatedSongs(int offset, int limit, string? songName = null, int? songNumber = null, string? categoryName = null, string? songBookId = null, List<string>? curatorIds = null, string? orderByColumn = null, CancellationToken cancellationToken = default);
        Task<ServiceResult<SongCollectionDto>> GetSongCollectionByIdAsync(string id);
        Task<ServiceResult<SongCollectionDto>> UpdateSongCollectionAsync(string id, SongCollectionDto updatedCollection);


    }
}