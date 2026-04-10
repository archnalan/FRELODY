using FRELODYAPIs.Areas.Admin.Interfaces;
using FRELODYLIB.ServiceHandler.ResultModels;
using FRELODYSHRD.Dtos.CreateDtos;
using System.Runtime.InteropServices;
using Tesseract;

namespace FRELODYAPIs.Areas.Admin.LogicData
{
    public class OcrService : IOcrService
    {
        private readonly ILogger<OcrService> _logger;

        public OcrService(ILogger<OcrService> logger, IWebHostEnvironment env, IConfiguration configuration)
        {
            _logger = logger;
        }

        public async Task<ServiceResult<OcrExtractResult>> ExtractTextFromImageAsync(byte[] imageData, string fileName)
        {
            if (imageData == null || imageData.Length == 0)
            {
                return ServiceResult<OcrExtractResult>.Failure(
                    new BadRequestException("No image data provided."));
            }
            try
            {
                _logger.LogInformation("[OCR] Starting extraction for {FileName} ({ImageSize} bytes)", fileName, imageData.Length);

                var (text, confidence) = await Task.Run(() => RunOcr(imageData));

                _logger.LogInformation("[OCR] Result — confidence: {Confidence:P0}, text length: {TextLength} chars, preview: {Preview}",
                    confidence, text.Length, text.Length > 120 ? text[..120] + "…" : text);

                return ServiceResult<OcrExtractResult>.Success(new OcrExtractResult
                {
                    ExtractedText = text,
                    Success = true,
                    Confidence = confidence
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OCR] Extraction failed for file: {FileName}", fileName);
                return ServiceResult<OcrExtractResult>.Failure(ex);
            }
        }

        private (string text, float confidence) RunOcr(byte[] imageData)
        {
            var tessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

            using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.LstmOnly);

            // Song sheets are a single block of text (chords + lyrics).
            // PSM 6 (single uniform block) avoids column-detection mistakes.
            // Disable dictionaries because chord symbols (Am, G7, Cmaj7) aren't
            // English words and the dictionary would "correct" them.
            engine.SetVariable("tessedit_pageseg_mode", "6");
            engine.SetVariable("load_system_dawg", "0");
            engine.SetVariable("load_freq_dawg", "0");

            using var img = Pix.LoadFromMemory(imageData);
            using var page = engine.Process(img, PageSegMode.SingleBlock);

            var text = page.GetText();
            var confidence = page.GetMeanConfidence();

            _logger.LogInformation("[OCR] Tesseract engine finished — confidence: {Confidence:P0}, raw text length: {RawLength}",
                confidence, text?.Length ?? 0);

            return (text?.Trim() ?? string.Empty, confidence);
        }
        
    }
}
