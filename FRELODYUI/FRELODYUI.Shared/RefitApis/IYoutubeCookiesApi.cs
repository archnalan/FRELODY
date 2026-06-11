using Refit;
using FRELODYSHRD.Dtos.YoutubeCookieDtos;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IYoutubeCookiesApi
    {
        [Get("/api/youtube-cookies/get-status")]
        Task<IApiResponse<CookieStatusDto>> GetStatus();

        [Post("/api/youtube-cookies/save-cookies")]
        Task<IApiResponse<SaveCookiesResultDto>> SaveCookies([Body] SaveCookiesRequestDto dto);

        [Delete("/api/youtube-cookies/delete-slot")]
        Task<IApiResponse<List<CookieSlotDto>>> DeleteSlot([Query] string name);
    }
}
