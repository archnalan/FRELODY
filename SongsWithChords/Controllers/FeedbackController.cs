using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYSHRD.ModelTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Areas.Admin.ApiControllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = $"{UserRoles.Editor},{UserRoles.Contributor},{UserRoles.Admin},{UserRoles.Owner}")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserFeedbackDto>), 200)]
        public async Task<IActionResult> GetAllFeedback()
        {
            var result = await _feedbackService.GetFeedbackAsync();

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(UserFeedbackDto), 200)]
        public async Task<IActionResult> GetFeedbackById([FromQuery] string id)
        {
            var result = await _feedbackService.GetFeedbackByIdAsync(id);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(UserFeedbackDto), 200)]
        public async Task<IActionResult> CreateFeedback([FromBody] UserFeedbackCreateDto feedback)
        {
            var result = await _feedbackService.CreateFeedbackAsync(feedback);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpPut]
        [ProducesResponseType(typeof(UserFeedbackDto), 200)]
        public async Task<IActionResult> UpdateFeedbackStatus([FromQuery] string id, [FromQuery] FeedbackStatus status)
        {
            var result = await _feedbackService.UpdateFeedbackStatusAsync(id, status);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> DeleteFeedback([FromQuery] string id)
        {
            var result = await _feedbackService.DeleteFeedbackAsync(id);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserFeedbackDto>), 200)]
        public async Task<IActionResult> GetFeedbackBySongId([FromQuery] string songId)
        {
            var result = await _feedbackService.GetFeedbackBySongIdAsync(songId);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserFeedbackDto>), 200)]
        public async Task<IActionResult> GetFeedbackByUserId([FromQuery] string userId)
        {
            var result = await _feedbackService.GetFeedbackByUserIdAsync(userId);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }
    }
}