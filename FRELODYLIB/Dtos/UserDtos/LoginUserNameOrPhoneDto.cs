using System.ComponentModel.DataAnnotations;

namespace SongsWithChords.Dtos.UserDtos
{
    public class LoginUserNameOrPhoneDto
    {
        public string? UserName { get; set; }

        [MinLength(4)]
        public string Password { get; set; }
        public string? PhoneNumber { get; set; }

    }
}
