namespace FRELODYAPP.Dtos.UserDtos
{
    public class UserClaimsDto
    {
        public string? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public List<string>? Roles { get; set; }
        public string? Package { get; set; }
        public string? TenantId { get; set; }

        public UserClaimsDto()
        {

        }

    }
}
