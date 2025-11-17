using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.UserDtos;

namespace FRELODYAPP.Dtos.UserDtos
{
    public class UserClaimsDto
    {
        public string? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public List<string>? Roles { get; set; }
        public string? Package { get; set; }
        public string? TenantId { get; set; }
        public string? Initials => 
            (string.IsNullOrEmpty(FirstName) ? "" : FirstName[0].ToString()) +
            (string.IsNullOrEmpty(LastName) ? "" : LastName[0].ToString());
        public UserType? UserType { get; set; }
        public BillingStatus? BillingStatus { get; set; }
        public UserClaimsDto()
        {

        }

    }
}
