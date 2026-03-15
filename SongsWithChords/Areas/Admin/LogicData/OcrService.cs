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
                var text = await Task.Run(() => RunOcr(imageData));

                return ServiceResult<OcrExtractResult>.Success(new OcrExtractResult
                {
                    ExtractedText = text,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR extraction failed for file: {FileName}", fileName);
                return ServiceResult<OcrExtractResult>.Failure(ex);
            }
        }

        private string RunOcr(byte[] imageData)
        {
            var tessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

            using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.LstmOnly);
            using var img = Pix.LoadFromMemory(imageData);
            using var page = engine.Process(img);

            var text = page.GetText();
            var confidence = page.GetMeanConfidence();

            _logger.LogInformation("OCR completed with confidence: {Confidence:P0}", confidence);

            return text?.Trim() ?? string.Empty;
        }
        
    }
}
