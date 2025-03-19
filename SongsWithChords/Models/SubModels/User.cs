using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SongsWithChords.Models.SubModels
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
        public DateTime? DateCreated { get; set; }

        public DateTime? DateModified { get; set; }

        public bool? IsDeleted { get; set; }

        public string? ModifiedBy { get; set; }

        public Guid? TenantId { get; set; }
    }
}
