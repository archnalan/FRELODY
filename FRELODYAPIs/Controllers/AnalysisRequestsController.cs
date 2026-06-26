using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    /// <summary>
    /// Superadmin review of analysis requests turned away at the access gate, plus the
    /// per-video whitelist that lets a popular over-long song be analyzed without
    /// raising the global duration cap.
    /// </summary>
    [Route("api/analysis-requests/[action]")]
    [ApiController]
    [Authorize(Roles = UserRoles.SuperAdmin)]
    public class AnalysisRequestsController : ControllerBase
    {
        private readonly IAnalysisRequestsService _requests;

        public AnalysisRequestsController(IAnalysisRequestsService requests)
        {
            _requests = requests;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<AnalysisRequestVideoDto>), 200)]
        public async Task<IActionResult> GetRequests()
        {
            var result = await _requests.GetRequestsAsync();

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<WhitelistedVideoDto>), 200)]
        public async Task<IActionResult> GetWhitelist()
        {
            var result = await _requests.GetWhitelistAsync();

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(AnalysisOutcomeStatsDto), 200)]
        public async Task<IActionResult> GetOutcomeStats([FromQuery] int days = 30)
        {
            var result = await _requests.GetOutcomeStatsAsync(days);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> ApproveVideo([FromBody] WhitelistVideoRequestDto request)
        {
            var result = await _requests.ApproveVideoAsync(request);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> RemoveWhitelist([FromBody] WhitelistVideoRequestDto request)
        {
            var result = await _requests.RemoveWhitelistAsync(request.Platform, request.VideoId);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }
    }
}
