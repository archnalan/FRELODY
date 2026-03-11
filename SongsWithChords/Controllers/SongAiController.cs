using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYSHRD.Dtos.CreateDtos;
using Microsoft.AspNetCore.Mvc;

namespace FRELODYAPIs.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SongAiController : ControllerBase
    {
        private readonly ISongAiService _songAiService;

        public SongAiController(ISongAiService songAiService)
        {
            _songAiService = songAiService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(List<SegmentCreateDto>), 200)]
        public async Task<IActionResult> RefineExtraction([FromBody] SongAiRefineRequest request)
        {
            var result = await _songAiService.RefineExtractionAsync(request.OriginalContent, request.Segments);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { message = result.Error.Message });

            return Ok(result.Data);
        }
    }
}
