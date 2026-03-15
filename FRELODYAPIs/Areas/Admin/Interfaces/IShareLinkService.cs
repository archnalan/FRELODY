using FRELODYAPP.Dtos;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FRELODYAPIs.Areas.Admin.Interfaces
{
    public interface IShareLinkService
    {
        Task<ServiceResult<ShareLinkDto>> GenerateShareLink([FromBody] ShareLinkCreateDto request, string? baseUrl);
        Task<ServiceResult<SongDto>> GetSharedSong([Required] string shareToken);
        Task<ServiceResult<bool>> RevokeShareLink([FromRoute] string shareToken);
    }
}