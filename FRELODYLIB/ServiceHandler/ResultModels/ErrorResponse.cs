namespace FRELODYLIB.ServiceHandler.ResultModels
{
    public class ErrorResponse
    {
        public string Title { get; set; } = string.Empty;
        public int Status { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }

    public class ValidationErrorResponse : ErrorResponse
    {
        public ValidationErrorResponse()
        {
            Title = "Validation Failed";
            Status = 400;
        }
    }
}