using FRELODYAPIs.Services.DocsMedia;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.DocsDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    // Documentation media is platform-global content. Only the platform SuperAdmin may
    // mutate it (matches the docs /admin/media page gate). GetManifest stays
    // [AllowAnonymous] so every docs visitor can render the published media.
    [Authorize(Roles = UserRoles.SuperAdmin)]
    [ApiController]
    public class DocMediaController : ControllerBase
    {
        private readonly IDocMediaService _docMedia;

        public DocMediaController(IDocMediaService docMedia)
        {
            _docMedia = docMedia;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(DocMediaManifestDto), 200)]
        public async Task<IActionResult> GetManifest(CancellationToken ct)
        {
            var result = await _docMedia.GetManifestAsync(ct);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6_000_000)]
        [ProducesResponseType(typeof(DocMediaEntryDto), 200)]
        public async Task<IActionResult> UploadImage([FromForm] string slot, IFormFile file, CancellationToken ct)
        {
            var result = await _docMedia.SaveImageAsync(slot, file, ct);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpPut]
        [ProducesResponseType(typeof(DocMediaEntryDto), 200)]
        public async Task<IActionResult> SetText([FromQuery] string slot, [FromBody] DocMediaTextUpdateDto dto, CancellationToken ct)
        {
            var result = await _docMedia.SetTextAsync(slot, dto, ct);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(DocMediaEntryDto), 200)]
        public async Task<IActionResult> Clear([FromQuery] string slot, [FromQuery] string kind, CancellationToken ct)
        {
            var result = await _docMedia.ClearAsync(slot, kind, ct);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, result.Error?.Message ?? "Error");
            return Ok(result.Data);
        }
    }
}
