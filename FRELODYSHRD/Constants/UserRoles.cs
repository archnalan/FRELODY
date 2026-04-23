using System;
using System.Collections.Generic;
using System.Linq;

namespace FRELODYSHRD.Constants
{
    /// <summary>
    /// Two-layer role model:
    ///   - Platform roles: system-wide (SuperAdmin, Support, User).
    ///   - Organization roles: scoped to a user's current organization
    ///     (Owner > Admin > Manager > Editor > Contributor > Viewer > Guest).
    /// All roles are stored in ASP.NET Identity's AspNetUserRoles. The
    /// <c>org_roles</c> JWT claim is the subset of a user's roles in
    /// <see cref="OrgRoles"/>; <c>platform_roles</c> is the subset in
    /// <see cref="PlatformRoles"/>.
    /// </summary>
    public static class UserRoles
    {
        // ------------------------------------------------------------
        // Platform-tier roles
        // ------------------------------------------------------------
        public const string SuperAdmin = "SuperAdmin";
        public const string Support    = "Support";
        public const string User       = "User";

        // ------------------------------------------------------------
        // Organization-tier roles
        // ------------------------------------------------------------
        public const string Owner       = "Owner";
        public const string Admin       = "Admin";
        public const string Manager     = "Manager";
        public const string Editor      = "Editor";
        public const string Contributor = "Contributor";
        public const string Viewer      = "Viewer";
        public const string Guest       = "Guest";

        /// <summary>
        /// Deprecated. Folded into <see cref="Admin"/>. Kept only to avoid
        /// breaking existing <c>[Authorize(Roles = ...)]</c> attribute strings;
        /// the role is no longer seeded so it has no effective members.
        /// </summary>
        [Obsolete("Use UserRoles.Admin. Will be removed in a future release.")]
        public const string Moderator = "Moderator";

        // ------------------------------------------------------------
        // Groupings
        // ------------------------------------------------------------
        public static readonly string[] PlatformRoles =
        {
            SuperAdmin, Support, User
        };

        public static readonly string[] OrgRoles =
        {
            Owner, Admin, Manager, Editor, Contributor, Viewer, Guest
        };

        /// <summary>
        /// Union of all currently supported roles (platform + org).
        /// </summary>
        public static readonly string[] AllRoles =
            PlatformRoles.Concat(OrgRoles).ToArray();

        /// <summary>
        /// Hierarchical rank for org roles. Higher number = more privilege.
        /// Use to gate "manage user with role X" actions: caller's rank must be
        /// strictly greater than the target's rank.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, int> OrgRoleRank =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [Owner]       = 70,
                [Admin]       = 60,
                [Manager]     = 50,
                [Editor]      = 40,
                [Contributor] = 30,
                [Viewer]      = 20,
                [Guest]       = 10,
            };

        public static bool IsOrgRole(string role) =>
            !string.IsNullOrEmpty(role) &&
            OrgRoles.Contains(role, StringComparer.OrdinalIgnoreCase);

        public static bool IsPlatformRole(string role) =>
            !string.IsNullOrEmpty(role) &&
            PlatformRoles.Contains(role, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns the highest rank among the given org roles, or 0 if none
        /// are org roles.
        /// </summary>
        public static int HighestOrgRank(IEnumerable<string>? roles)
        {
            if (roles == null) return 0;
            int max = 0;
            foreach (var r in roles)
            {
                if (r != null && OrgRoleRank.TryGetValue(r, out var rank) && rank > max)
                    max = rank;
            }
            return max;
        }

        /// <summary>True if <paramref name="callerRoles"/> outranks every org role in
        /// <paramref name="targetRoles"/>.</summary>
        public static bool CanManage(IEnumerable<string>? callerRoles, IEnumerable<string>? targetRoles)
            => HighestOrgRank(callerRoles) > HighestOrgRank(targetRoles);
    }
}
