using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;

namespace FRELODYAPIs.Authorization
{
    /// <summary>
    /// Convenience attribute that maps to a dynamically-named authorization policy
    /// requiring the caller to hold any of the supplied org-tier roles.
    /// Usage: <c>[OrgRole(UserRoles.Owner, UserRoles.Admin)]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class OrgRoleAttribute : AuthorizeAttribute
    {
        public const string PolicyPrefix = "OrgRole:";

        public OrgRoleAttribute(params string[] orgRoles)
        {
            // Encode the required roles into the policy name so the
            // OrgRolePolicyProvider can synthesize the requirement on demand.
            Policy = PolicyPrefix + string.Join(",", orgRoles ?? Array.Empty<string>());
        }

        public static string[] ExtractRoles(string policyName)
        {
            if (string.IsNullOrEmpty(policyName) || !policyName.StartsWith(PolicyPrefix))
                return Array.Empty<string>();
            var csv = policyName.Substring(PolicyPrefix.Length);
            return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
