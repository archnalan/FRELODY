namespace FRELODYAPP.Dtos.UserDtos
{
    public class CreateUserResponseDto
    {
        public string? Id { get; set; }

        public string UserName { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }
        public string? Address { get; set; }
        public string? AboutMe { get; set; }
        public string? ProfilePicUrl { get; set; }
        public string? CoverPhotoUrl { get; set; }
        public List<string>? DefaultRoles { get; set; }

    }
}
