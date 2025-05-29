namespace FRELODYAPP.Dtos.AuthDtos
{
    public class LoginResponseDto
    {
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public string? TenantId { get; set; }
        public DateTime? Expiry { get; set; }
    }
}
