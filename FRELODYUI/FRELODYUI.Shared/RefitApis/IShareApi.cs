using FRELODYAPP.Dtos;
using FRELODYSHRD.Dtos;
using FRELODYUI.Shared.Models.PlaylistModels;
using Refit;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IShareApi
    {
        [Post("/api/share/generate-share-link")]
        Task<IApiResponse<ShareLinkDto>> GenerateShareLink([Body] ShareLinkCreateDto request);

        [Get("/api/share/get-shared-song")]
        Task<IApiResponse<SongDto>> GetSharedSong([Query] string shareToken);

        [Get("/api/share/get-shared-playlist")]
        Task<IApiResponse<PlaylistSongs>> GetSharedPlaylist([Query] string shareToken);

        [Delete("/api/share/revoke-share-link")]
        Task<IApiResponse<bool>> RevokeShareLink([Query] string shareToken);
    }
}

