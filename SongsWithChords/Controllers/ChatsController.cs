using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYSHRD.Dtos.ChatDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ChatsController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatsController> _logger;

        public ChatsController(IChatService chatService, ILogger<ChatsController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ChatSessionDto), 200)]
        public async Task<IActionResult> CreateAnonymousChatSession()
        {
            var result = await _chatService.CreateAnonymousChatSessionAsync();

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ChatMessageDto), 200)]
        public async Task<IActionResult> SendMessage(
            [FromQuery] string sessionId,
            [FromQuery] string message,
            [FromQuery] bool isFromAdmin = false)
        {
            if (string.IsNullOrWhiteSpace(message))
                return BadRequest(new { message = "Message cannot be empty." });

            var result = await _chatService.SendMessageAsync(sessionId, message, isFromAdmin);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ChatSessionDto>), 200)]
        public async Task<IActionResult> GetActiveSessions()
        {
            var result = await _chatService.GetActiveSessionsAsync();

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ChatMessageDto>), 200)]
        public async Task<IActionResult> GetSessionMessages([FromQuery] string sessionId)
        {
            var result = await _chatService.GetSessionMessagesAsync(sessionId);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }

        [HttpPut]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> CloseSession([FromQuery] string sessionId)
        {
            var result = await _chatService.CloseSessionAsync(sessionId);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }
    }
}