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
        private readonly IOcrService _ocrService;
        public SongAiController(ISongAiService songAiService, IOcrService ocrService)
        {
            _songAiService = songAiService;
            _ocrService = ocrService;
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

        [HttpPost]
        [ProducesResponseType(typeof(OcrExtractResult), 200)]
        public async Task<IActionResult> OcrExtract([FromBody] OcrExtractRequest request)
        {
            if (string.IsNullOrEmpty(request.ImageBase64))
                return BadRequest(new { message = "No image data provided." });

            byte[] imageData;
            try
            {
                imageData = Convert.FromBase64String(request.ImageBase64);
            }
            catch (FormatException)
            {
                return BadRequest(new { message = "Invalid base64 image data." });
            }

            var ocrResult = await _ocrService.ExtractTextFromImageAsync(imageData, request.FileName);

            if (!ocrResult.IsSuccess)
                return StatusCode(ocrResult.StatusCode, new { message = ocrResult.Error.Message });

            return Ok(ocrResult.Data);
        }
    }
}
