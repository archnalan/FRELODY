using Azure;
using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYSHRD.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ShareController : ControllerBase
    {
        private readonly IShareLinkService _shareLinkService;
        public ShareController(IShareLinkService shareLinkService)
        {
            _shareLinkService = shareLinkService;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateShareLink([FromBody] ShareLinkCreateDto request)
        {
            var result = await _shareLinkService.GenerateShareLink(request, GetBaseUrl());
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error.Message);
            }
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetSharedSong([FromQuery] string shareToken)
        {
            var result = await _shareLinkService.GetSharedSong(shareToken);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error.Message);
            }
            var json = System.Text.Json.JsonSerializer.Serialize(result.Data);
            Console.WriteLine("Shared Song JSON: " + json);
            return Ok(result.Data);
        }

        [HttpDelete]
        public async Task<IActionResult> RevokeShareLink([FromQuery] string shareToken)
        {
            var request = HttpContext.Request;
            var result = await _shareLinkService.RevokeShareLink(shareToken);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Error.Message);
            }
            return Ok(result);
        }

        private string GetBaseUrl()
        {
            var request = HttpContext.Request;
            return $"{request.Scheme}://{request.Host}";
        }
    }
}
