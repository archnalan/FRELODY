using System.ComponentModel.DataAnnotations;

namespace SongsWithChords.Dtos.UserDtos
{
    public class UserLogin
    {
        [Required]
        public string Email { get; set; }
        [Required]
        [MinLength(4)]
        public string Password { get; set; }

    }
}
