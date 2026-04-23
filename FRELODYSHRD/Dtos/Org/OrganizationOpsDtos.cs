using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FRELODYSHRD.Dtos.Org
{
    /// <summary>Row in the org-admin Members table.</summary>
    public class OrganizationMemberRowDto
    {
        public string Id { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName => $"{FirstName} {LastName}".Trim();
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePicUrl { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public bool MustChangePassword { get; set; }
        /// <summary>True until the invited user accepts (e.g. signs in for the
        /// first time / completes their forced password change).</summary>
        public bool InvitationPending { get; set; }
        public DateTimeOffset? LastLoginDate { get; set; }
        public DateTimeOffset? DateCreated { get; set; }
    }

    /// <summary>High-level metric tiles for the org dashboard.</summary>
    public class OrgMetricsDto
    {
        public int Members { get; set; }
        public int ActiveMembers { get; set; }
        public int Admins { get; set; }
        public int Songs { get; set; }
        public int Playlists { get; set; }
        public int SongBooks { get; set; }
        public int Albums { get; set; }
    }

    /// <summary>Lightweight rollup feeding the dashboard's activity card.</summary>
    public class OrgActivitySummaryDto
    {
        public int SongsCreatedLast7Days { get; set; }
        public int PlaylistsCreatedLast7Days { get; set; }
        public int NewMembersLast7Days { get; set; }
        public DateTimeOffset? LastContentCreatedAt { get; set; }
        public string? LastContentCreatedByName { get; set; }
    }

    public class CreateOrganizationDto
    {
        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Industry { get; set; }
    }

    /// <summary>Admin creates a brand-new member account inside the current org.</summary>
    public class CreateOrgMemberDto
    {
        [Required] public string FirstName { get; set; } = string.Empty;
        [Required] public string LastName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? UserName { get; set; }

        /// <summary>Optional explicit temp password. If null, the server generates one and emails it.</summary>
        public string? TemporaryPassword { get; set; }

        /// <summary>Org roles to assign (e.g. <c>["Editor"]</c>). Defaults to <c>Viewer</c> when empty.</summary>
        public List<string>? OrgRoles { get; set; }
    }

    /// <summary>Admin invites a user that already exists on the platform.</summary>
    public class InviteExistingMemberDto
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        public List<string>? OrgRoles { get; set; }
        /// <summary>If true, override the destructive warning when the invitee already
        /// belongs to another org (caller has already shown the warning UI).</summary>
        public bool ConfirmContentForfeit { get; set; }
    }

    /// <summary>Returned to the client when accepting an invite would cause the
    /// invitee to lose access to content created in their current org.</summary>
    public class JoinOrgWarningDto
    {
        public bool RequiresContentForfeitConfirmation { get; set; }
        public string? CurrentOrgName { get; set; }
        public string? TargetOrgName { get; set; }
        public OrgContentCounts ContentCounts { get; set; } = new();
    }

    public class OrgContentCounts
    {
        public int Songs { get; set; }
        public int Playlists { get; set; }
        public int SongBooks { get; set; }
        public int Albums { get; set; }
        public int Total => Songs + Playlists + SongBooks + Albums;
    }

    public class TransferOwnershipDto
    {
        [Required] public string NewOwnerUserId { get; set; } = string.Empty;
    }

    public class ChangeMemberRolesDto
    {
        [Required] public string UserId { get; set; } = string.Empty;
        [Required] public List<string> OrgRoles { get; set; } = new();
    }

    public class AcceptInviteDto
    {
        [Required] public string Token { get; set; } = string.Empty;
        public bool ConfirmContentForfeit { get; set; }
    }

    public class SwitchOrganizationDto
    {
        [Required] public string TargetOrganizationId { get; set; } = string.Empty;
        public bool ConfirmContentForfeit { get; set; }
    }
}
