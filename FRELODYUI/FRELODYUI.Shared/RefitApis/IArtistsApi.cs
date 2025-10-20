using FRELODYSHRD.Dtos;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IArtistsApi
    {
        [Get("/api/artists/get-all-artists")]
        Task<IApiResponse<List<ArtistDto>>> GetAllArtists();

        [Get("/api/artists/get-artist-by-id")]
        Task<IApiResponse<ArtistDto>> GetArtistById([Query] string id);

        [Post("/api/artists/create-artist")]
        Task<IApiResponse<ArtistDto>> CreateArtist([Body] ArtistDto artistDto);

        [Delete("/api/artists/delete-artist")]
        Task<IApiResponse<bool>> DeleteArtist([Query] string id);
    }
}