using FRELODYAPP.Data;
using FRELODYAPP.Dtos.AuthDtos;
using FRELODYLIB.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SmtpSenderController : ControllerBase
    {
        private readonly ISmtpSenderService _smtpSenderService;

        public SmtpSenderController(ISmtpSenderService smtpSenderService)
        {
            _smtpSenderService = smtpSenderService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> SendMail([FromBody] EmailDto emailDto)
        {
            var result = await _smtpSenderService.SendMailAsync(emailDto);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> SendDeveloperNotificationEmail([FromQuery] string message)
        {
            var result = await _smtpSenderService.SendDeveloperNotificationEmailAsync(message);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }

        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> SendPasswordResetEmail([FromQuery] string userEmail, [FromQuery] string requestorUri)
        {
            var result = await _smtpSenderService.SendPasswordResetEmailAsync(userEmail, requestorUri);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error);
            }
            return Ok(result.Data);
        }
    }
}
