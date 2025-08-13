using FRELODYAPP.Dtos;
using FRELODYSHRD.Dtos;
using Refit;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IShareApi
    {
        [Post("/api/share/generate-share-link")]
        Task<IApiResponse<ShareLinkDto>> GenerateShareLink([Body] ShareLinkCreateDto request);

        [Get("/api/share/get-shared-song/{shareToken}")]
        Task<IApiResponse<SongDto>> GetSharedSong([AliasAs("shareToken")] string shareToken);
    }
}

