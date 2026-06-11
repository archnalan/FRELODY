using FRELODYAPIs.Services.YoutubeCookies;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.YoutubeCookieDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [Authorize(Roles = UserRoles.SuperAdmin)]
    [ApiController]
    public class YoutubeCookiesController : ControllerBase
    {
        private readonly IYoutubeCookieService _cookies;

        public YoutubeCookiesController(IYoutubeCookieService cookies)
        {
            _cookies = cookies;
        }

        [HttpGet]
        [ProducesResponseType(typeof(CookieStatusDto), 200)]
        public async Task<IActionResult> GetStatus(CancellationToken ct)
        {
            var result = await _cookies.GetStatusAsync(ct);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(SaveCookiesResultDto), 200)]
        public async Task<IActionResult> SaveCookies([FromBody] SaveCookiesRequestDto dto, CancellationToken ct)
        {
            var result = await _cookies.SaveCookiesAsync(dto, ct);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(List<CookieSlotDto>), 200)]
        public async Task<IActionResult> DeleteSlot([FromQuery] string name, CancellationToken ct)
        {
            var result = await _cookies.DeleteSlotAsync(name, ct);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }
    }
}
