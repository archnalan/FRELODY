using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.YoutubeCookieDtos;

namespace FRELODYAPIs.Services.YoutubeCookies
{
    public interface IYoutubeCookieService
    {
        /// <summary>Returns the last status published by the cookie-refresher sidecar. Safe for SuperAdmin only.</summary>
        Task<ServiceResult<CookieStatusDto>> GetStatusAsync(CancellationToken ct = default);

        /// <summary>Validates and persists a Netscape cookie export as a new rotation slot. Prunes to MaxSlots.</summary>
        Task<ServiceResult<SaveCookiesResultDto>> SaveCookiesAsync(SaveCookiesRequestDto req, CancellationToken ct = default);

        /// <summary>Deletes a named seed slot. Returns the remaining slot list.</summary>
        Task<ServiceResult<List<CookieSlotDto>>> DeleteSlotAsync(string name, CancellationToken ct = default);
    }
}
