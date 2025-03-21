namespace FRELODYAPP.Dtos.UserDtos
{
    public class UpdateUserProfileOutDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string? Relationship { get; set; }
        public string? Address { get; set; }
        public string? Family { get; set; }
        public string? Profession { get; set; }
        public string? AboutMe { get; set; }
        public string? Contacts { get; set; }
        public string ProfilePicUrl { get; set; }
        public string CoverPicUrl { get; set; }
        public int TotalSongs { get; set; }

        public UpdateUserProfileOutDto()
        {

        }

    }
}
