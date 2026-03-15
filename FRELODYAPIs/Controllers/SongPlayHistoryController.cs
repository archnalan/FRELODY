using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYAPP.Dtos;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.HybridDtos;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Areas.Admin.ApiControllers
{
    [Route("api/song-play-history/[action]")]
    [ApiController]
    public class SongPlayHistoryController : ControllerBase
    {
        private readonly ISongPlayHistoryService _playLog;

        public SongPlayHistoryController(ISongPlayHistoryService playLog)
        {
            _playLog = playLog;
        }

        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> LogSongPlay([FromQuery] string songId, [FromQuery] string? playSource = null)
        {
            var result = await _playLog.LogSongPlay(songId, playSource);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<SongPlayHistoryDto>), 200)]
        public async Task<IActionResult> GetUserPlayHistory([FromQuery] string? userId = null, [FromQuery] int offset = 0, [FromQuery] int limit = 10)
        {
            var result = await _playLog.GetUserSongPlayHistory(userId, offset, limit);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<SongPlayHistoryDto>), 200)]
        public async Task<IActionResult> GetSongPlayHistory([FromQuery] string songId, [FromQuery] int offset = 0, [FromQuery] int limit = 10)
        {
            var result = await _playLog.GetSongPlayHistory(songId, offset, limit);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(Dictionary<string, int>), 200)]
        public async Task<IActionResult> GetMostPlayedSongs([FromQuery] string? userId = null, [FromQuery] int limit = 10)
        {
            var result = await _playLog.GetMostPlayedSongs(userId, limit);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(SongPlayStatisticsDto), 200)]
        public async Task<IActionResult> GetPlayStatistics([FromQuery] string? userId = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            var result = await _playLog.GetPlayStatistics(userId, fromDate, toDate);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<SongPlayHistoryDto>), 200)]
        public async Task<IActionResult> GetRecentPlays([FromQuery] string? userId = null, [FromQuery] int limit = 5)
        {
            var result = await _playLog.GetRecentPlays(userId, limit);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> ClearUserHistory([FromQuery] string? userId = null)
        {
            var result = await _playLog.ClearUserHistory(userId);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }
    }
}