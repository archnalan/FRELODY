using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace FRELODYAPIs.Authorization
{
    /// <summary>
    /// Materializes <c>OrgRole:&lt;roles&gt;</c> policies on demand so we don't have
    /// to register one policy per role combination.
    /// </summary>
    public sealed class OrgRolePolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback;

        public OrgRolePolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallback = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()  => _fallback.GetDefaultPolicyAsync();
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (!string.IsNullOrEmpty(policyName) && policyName.StartsWith(OrgRoleAttribute.PolicyPrefix))
            {
                var roles = OrgRoleAttribute.ExtractRoles(policyName);
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new OrgRoleRequirement(roles))
                    .Build();
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            return _fallback.GetPolicyAsync(policyName);
        }
    }
}
