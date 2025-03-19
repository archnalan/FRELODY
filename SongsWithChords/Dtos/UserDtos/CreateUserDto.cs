using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SongsWithChords.Dtos.UserDtos
{
    public class CreateUserDto
    {
        [Required]
        [MinLength(4)]
        public string Password { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string Lastname { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }
        public string? AboutMe { get; set; }
        public string? ProfilePicUrl { get; set; }
        public string? CoverPhotoUrl { get; set; }

        [NotMapped]
        public List<string>? DefaultRoles { get; set; }
    }
}
