using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.UserDtos;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models.SubModels
{
    public class User : IdentityUser, IBaseEntity
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string? Address { get; set; }

        public string? AboutMe { get; set; }

        public string? Contact { get; set; }

        public string? ProfilePicUrl { get; set; }

        public string? CoverPhotoUrl { get; set; }

        [NotMapped]
        public List<string>? Roles { get; set; }        
        public DateTimeOffset? DateCreated { get; set; }

        public DateTimeOffset? DateModified { get; set; }

        public UserType? UserType { get; set; }

        public bool? IsDeleted { get; set; }

        public bool? IsActive { get; set; }

        public string? CreatedBy { get; set; }

        public string? ModifiedBy { get; set; }

        public string? TenantId { get; set; }

        public BillingStatus? BillingStatus { get; set; } 

        public DateTimeOffset? LastLoginDate { get; set; }
    }

}
