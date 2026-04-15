using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYSHRD.Dtos.AuthDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OtpController : ControllerBase
    {
        private readonly IOtpService _otpService;

        public OtpController(IOtpService otpService)
        {
            _otpService = otpService;
        }

        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SendOtpResponseDto), 200)]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto request)
        {
            var result = await _otpService.SendOtp(request);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(VerifyOtpResponseDto), 200)]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
        {
            var result = await _otpService.VerifyOtp(request);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }

        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SendOtpResponseDto), 200)]
        public async Task<IActionResult> ResendOtp([FromQuery] string email)
        {
            var result = await _otpService.ResendOtp(email);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });
            return Ok(result.Data);
        }
    }
}
