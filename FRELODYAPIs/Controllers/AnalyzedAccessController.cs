using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYSHRD.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/analyzed-access/[action]")]
    [ApiController]
    [Authorize]
    public class AnalyzedAccessController : ControllerBase
    {
        private readonly IAnalyzedAccessService _access;

        public AnalyzedAccessController(IAnalyzedAccessService access)
        {
            _access = access;
        }

        /// <summary>
        /// Evaluate + consume a daily slot for an analyzed song. Returns 200 with the
        /// access result even when not allowed, so the client can render the in-context
        /// paywall sheet from <see cref="AnalyzedAccessResultDto.LimitReached"/>.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AnalyzedAccessResultDto), 200)]
        public async Task<IActionResult> Unlock([FromBody] AnalyzedAccessRequest request)
        {
            var result = await _access.EvaluateAndRecord(
                request.Platform, request.VideoId, request.Title, request.ThumbnailUrl);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        /// <summary>Public monetization limits so the client can pre-gate over-long content.</summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AnalyzedLimitsDto), 200)]
        public IActionResult Limits() => Ok(_access.GetLimits());

        [HttpGet]
        [ProducesResponseType(typeof(AnalyzedAccessResultDto), 200)]
        public async Task<IActionResult> QuotaStatus()
        {
            var result = await _access.GetQuotaStatus();

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<AnalyzedSongDto>), 200)]
        public async Task<IActionResult> TodaysSongs()
        {
            var result = await _access.GetTodaysSongs();

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        /// <summary>
        /// Last 7 days of song history with accessibility flags, quota info, practice streak,
        /// and a 30-day daily activity map for the calendar heatmap.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(SongHistoryDto), 200)]
        public async Task<IActionResult> SongHistory()
        {
            var result = await _access.GetSongHistory();

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }
    }
}
