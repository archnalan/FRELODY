using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Dtos.UserDtos
{
    public class EditUserProfile
    {
        public string? Id { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public string? Address { get; set; }
        public string? AboutMe { get; set; }
        public string? Profession { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePicUrl { get; set; }
        public string? CoverPhotoUrl { get; set; }
        public string? TenantId { get; set; }

        [NotMapped]
        public List<string>? AssignedRoles { get; set; }

    }
    public class UpdateUserProfile : EditUserProfile
    {

        [Required]
        [MinLength(4)]
        public string Password { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required]
        public string UserName { get; set; }

    }
}
