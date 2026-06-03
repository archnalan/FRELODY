using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYLIB.ServiceHandler;
using FRELODYSHRD.Dtos;
using FRELODYSHRD.Dtos.CreateDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class MyFeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public MyFeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyFeedback()
        {
            var result = await _feedbackService.GetMyFeedbackAsync();

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> HasFeedback()
        {
            var result = await _feedbackService.HasMyFeedbackAsync();

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(PaginationDetails<UserFeedbackDto>), 200)]
        public async Task<IActionResult> GetMyFeedbackPaged(
            [FromQuery] string? keywords = null,
            [FromQuery] int offSet = 0,
            [FromQuery] int limit = 20,
            [FromQuery] string sortByColumn = "DateCreated",
            [FromQuery] bool sortAscending = false,
            CancellationToken cancellationToken = default)
        {
            var result = await _feedbackService.GetMyFeedbackPagedAsync(
                keywords, offSet, limit, sortByColumn, sortAscending, cancellationToken);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Reply([FromQuery] string id, [FromBody] FeedbackReplyCreateDto reply)
        {
            var result = await _feedbackService.AddUserReplyAsync(id, reply.Body);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }
    }
}
