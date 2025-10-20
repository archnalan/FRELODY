using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface IArtistService
    {
        Task<ServiceResult<List<ArtistDto>>> GetAllArtists();
        Task<ServiceResult<ArtistDto>> GetArtistById(string id);
        Task<ServiceResult<ArtistDto>> CreateArtist(ArtistDto artistDto);
        Task<ServiceResult<bool>> DeleteArtist(string id);
    }
}