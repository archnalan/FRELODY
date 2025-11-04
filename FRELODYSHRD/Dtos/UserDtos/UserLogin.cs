using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Dtos.UserDtos
{
    public class UserLogin
    {
        [Required]
        public string Email { get; set; }
        [Required]
        [MinLength(4)]
        public string Password { get; set; }
        [Phone]
        public string? PhoneNumber { get; set; } 

    }
}
