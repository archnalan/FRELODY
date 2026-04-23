using FRELODYSHRD.Constants;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FRELODYAPIs.Authorization
{
    /// <summary>
    /// Authorization requirement: caller's <c>org_roles</c> JWT claim must contain
    /// at least one of the roles passed to <see cref="OrgRoleAttribute"/>.
    /// SuperAdmin / platform-tier <c>SuperAdmin</c> bypasses every check.
    /// </summary>
    public sealed class OrgRoleRequirement : IAuthorizationRequirement
    {
        public OrgRoleRequirement(params string[] requiredOrgRoles)
        {
            RequiredOrgRoles = requiredOrgRoles ?? Array.Empty<string>();
        }

        public string[] RequiredOrgRoles { get; }
    }

    public sealed class OrgRoleAuthorizationHandler : AuthorizationHandler<OrgRoleRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OrgRoleRequirement requirement)
        {
            var user = context.User;
            if (user?.Identity?.IsAuthenticated != true)
                return Task.CompletedTask;

            // SuperAdmin always wins.
            if (user.IsInRole(UserRoles.SuperAdmin) ||
                string.Equals(user.FindFirst("UserType")?.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var orgRolesCsv = user.FindFirst("org_roles")?.Value ?? string.Empty;
            var orgRoles = orgRolesCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (requirement.RequiredOrgRoles.Length == 0 && orgRoles.Length > 0)
            {
                // No specific role required \u2014 any org-tier role suffices.
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            foreach (var role in requirement.RequiredOrgRoles)
            {
                if (orgRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }
    }
}
