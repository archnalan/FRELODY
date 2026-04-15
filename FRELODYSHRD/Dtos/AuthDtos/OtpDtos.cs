namespace FRELODYSHRD.Dtos.AuthDtos
{
    public class SendOtpRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? TenantName { get; set; }
    }

    public class SendOtpResponseDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class VerifyOtpRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }

    public class VerifyOtpResponseDto
    {
        public bool Verified { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
