using FRELODYSHRD.Dtos.UserDtos;

namespace FRELODYAPP.Dtos.AuthDtos
{
    public class LoginResponseDto
    {
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public string? TenantId { get; set; }
        public DateTime? Expiry { get; set; }
        public UserType? UserType { get; set; }
    }
}
