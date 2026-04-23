using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.UserDtos;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>Subset of <see cref="Roles"/> that are organization-tier roles.</summary>
        public List<string>? OrgRoles { get; set; }

        /// <summary>Subset of <see cref="Roles"/> that are platform-tier roles.</summary>
        public List<string>? PlatformRoles { get; set; }

        /// <summary>True when the user must change their password before continuing
        /// (e.g. admin-created accounts with a temporary password).</summary>
        public bool MustChangePassword { get; set; }

        /// <summary>Convenience: name of the user's current organization, if any.</summary>
        public string? OrganizationName { get; set; }

        public string? Initials => 
            (string.IsNullOrEmpty(FirstName) ? "" : FirstName[0].ToString()) +
            (string.IsNullOrEmpty(LastName) ? "" : LastName[0].ToString());
        public UserType? UserType { get; set; }
        public BillingStatus? BillingStatus { get; set; }
        public UserClaimsDto()
        {

        }

        /// <summary>Recomputes <see cref="OrgRoles"/> and <see cref="PlatformRoles"/> from <see cref="Roles"/>.</summary>
        public void SyncRoleBuckets()
        {
            OrgRoles = Roles?.Where(UserRoles.IsOrgRole).ToList() ?? new List<string>();
            PlatformRoles = Roles?.Where(UserRoles.IsPlatformRole).ToList() ?? new List<string>();
        }
    }
}
