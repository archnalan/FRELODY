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
        private readonly ILogger<SongAiController> _logger;
        public SongAiController(ISongAiService songAiService, IOcrService ocrService, ILogger<SongAiController> logger)
        {
            _songAiService = songAiService;
            _ocrService = ocrService;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(List<SegmentCreateDto>), 200)]
        public async Task<IActionResult> RefineExtraction([FromBody] SongAiRefineRequest request)
        {
            var result = await _songAiService.RefineExtractionAsync(request.OriginalContent, request.Segments, request.ImageBase64);

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

            var data = ocrResult.Data!;

            // Auto-refine with AI vision when OCR output is garbled (e.g. handwritten text)
            var isGarbled = IsGarbledOcrText(data.ExtractedText, data.Confidence, out var junkRatio);
            _logger.LogInformation(
                "[OCR→AI] Garble check — confidence: {Confidence:P0}, junkRatio: {JunkRatio:P1}, isGarbled: {IsGarbled}",
                data.Confidence, junkRatio, isGarbled);

            if (isGarbled)
            {
                _logger.LogInformation("[OCR→AI] Auto-refining garbled OCR text ({TextLength} chars) with vision model", data.ExtractedText.Length);
                var refined = await _songAiService.RefineOcrTextAsync(data.ExtractedText, request.ImageBase64);
                if (!string.IsNullOrWhiteSpace(refined))
                {
                    _logger.LogInformation(
                        "[OCR→AI] AI refinement succeeded — original: {OriginalLength} chars → refined: {RefinedLength} chars, preview: {Preview}",
                        data.ExtractedText.Length, refined.Length, refined.Length > 120 ? refined[..120] + "…" : refined);
                    data.ExtractedText = refined;
                    data.WasAutoRefined = true;
                }
                else
                {
                    _logger.LogWarning("[OCR→AI] AI refinement returned empty — keeping original OCR text");
                }
            }

            return Ok(data);
        }

        private static bool IsGarbledOcrText(string text, float confidence, out float junkRatio)
        {
            junkRatio = 0f;

            // Very low confidence is a strong garble signal
            if (confidence < 0.40f) return true;

            if (string.IsNullOrWhiteSpace(text) || text.Length < 10) return true;

            // Count junk symbols that aren't part of normal chord/lyric text
            int junkCount = 0;
            int totalNonWhitespace = 0;

            foreach (var c in text)
            {
                if (char.IsWhiteSpace(c)) continue;
                totalNonWhitespace++;

                if (!char.IsLetterOrDigit(c) &&
                    c != '-' && c != '\'' && c != ',' && c != '.' &&
                    c != ':' && c != '/' && c != '#' && c != '[' && c != ']' &&
                    c != '(' && c != ')' && c != ';' && c != '!' && c != '?')
                    junkCount++;
            }

            if (totalNonWhitespace == 0) return true;

            junkRatio = (float)junkCount / totalNonWhitespace;

            // Medium confidence combined with high junk ratio
            return confidence < 0.60f && junkRatio > 0.10f;
        }
    }
}
