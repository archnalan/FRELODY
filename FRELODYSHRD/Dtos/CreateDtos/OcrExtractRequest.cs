namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class OcrExtractRequest
    {
        public string ImageBase64 { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }

    public class OcrExtractResult
    {
        public string ExtractedText { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
